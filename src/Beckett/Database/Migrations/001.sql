create type new_stream_event as
(
  id uuid,
  type text,
  data jsonb,
  metadata jsonb
);

create type stream_event as
(
  id uuid,
  stream_name text,
  stream_position bigint,
  global_position bigint,
  type text,
  data text,
  metadata text,
  timestamp timestamp with time zone
);

create or replace function stream_category(
  _stream_name text
)
  returns text
  immutable
  language sql
as
$$
select split_part(_stream_name, '-', 1);
$$
;

create table if not exists events
(
  id uuid not null unique,
  global_position bigint generated always as identity primary key,
  stream_position bigint not null,
  transaction_id xid8 default pg_current_xact_id() not null,
  timestamp timestamp with time zone default now() not null,
  stream_name text not null,
  type text not null,
  data jsonb not null,
  metadata jsonb,
  unique (stream_name, stream_position)
);

create index ix_events_stream_category on events (stream_category(stream_name));

create index if not exists ix_events_type on events (type);

create table if not exists subscriptions
(
  name text not null primary key,
  event_types text[] not null,
  initialized boolean default false not null
);

create table if not exists subscription_streams
(
  stream_position bigint default 0 not null,
  checkpoint bigint default 0 not null,
  blocked boolean default false not null,
  subscription_name text not null,
  stream_name text not null,
  primary key (subscription_name, stream_name)
);

create function stream_hash(
  _stream_name text
)
  returns bigint
  immutable
  language sql
as
$$
select abs(hashtextextended(_stream_name, 0));
$$;

create function append_to_stream(
  _stream_name text,
  _expected_version bigint,
  _events new_stream_event[],
  _send_poll_notification boolean default false
)
  returns bigint
  language plpgsql
as
$$
declare
  _current_version bigint;
  _stream_version bigint;
begin
  perform pg_advisory_xact_lock(stream_hash(_stream_name));

  select coalesce(max(e.stream_position), 0)
  into _current_version
  from events e
  where e.stream_name = _stream_name;

  if (_expected_version < -2) then
    raise exception 'Invalid value for expected version: %', _expected_version;
  end if;

  if (_expected_version = -1 and _current_version = 0) then
    raise exception 'Attempted to append to a non-existing stream: %', _stream_name;
  end if;

  if (_expected_version = 0 and _current_version > 0) then
    raise exception 'Attempted to start a stream that already exists: %', _stream_name;
  end if;

  if (_expected_version > 0 and _expected_version != _current_version) then
    raise exception 'Stream % version % does not match expected version %',
      _stream_name,
      _current_version,
      _expected_version;
  end if;

  with append_events as (
    insert into events (
      id,
      stream_position,
      stream_name,
      type,
      data,
      metadata
    )
    select e.id,
           _current_version + (row_number() over())::bigint,
           _stream_name,
           e.type,
           e.data,
           e.metadata
    from unnest(_events) as e
    returning stream_position, type
  ),
  new_stream_version as (
    select max(stream_position) as stream_version
    from append_events
  ),
  record_subscription_streams as (
    insert into event_store.subscription_streams (subscription_name, stream_name, stream_position)
    select s.name, _stream_name, v.stream_version
    from new_stream_version v, event_store.subscriptions s
    inner join append_events e on e.type = any (s.event_types)
    on conflict (subscription_name, stream_name) do update
      set stream_position = excluded.stream_position
  )
  select stream_version into _stream_version
  from new_stream_version;

  if (_send_poll_notification = true) then
    perform pg_notify('beckett:poll', null);
  end if;

  return _stream_version;
end;
$$;

--TODO: support expected version, return actual stream version regardless of filters
create function read_stream(
  _stream_name text,
  _expected_version bigint default null,
  _starting_stream_position bigint default null,
  _ending_global_position bigint default null,
  _count integer default null,
  _read_forwards boolean default true
)
  returns setof stream_event
  language sql
as
$$
select e.id,
       e.stream_name,
       e.stream_position,
       e.global_position,
       e.type,
       e.data,
       e.metadata,
       e.timestamp
from events e
where e.stream_name = _stream_name
and (_starting_stream_position is null or e.stream_position >= _starting_stream_position)
and (_ending_global_position is null or e.global_position <= _ending_global_position)
order by case when _read_forwards = true then stream_position end,
         case when _read_forwards = false then stream_position end desc
limit _count;
$$;

create function add_or_update_subscription(
  _subscription_name text,
  _event_types text[],
  _start_from_beginning boolean
)
  returns void
  language plpgsql
as
$$
declare
  _initialized boolean;
begin
  insert into subscriptions (name, event_types)
  values (_subscription_name, _event_types)
  on conflict (name) do update set event_types = excluded.event_types
  returning initialized into _initialized;

  if (_initialized = true) then
    return;
  end if;

  with matching_streams as (
    select stream_name, max(stream_position) as stream_position
    from events
    where type = any(_event_types)
    group by stream_name
  )
  insert into subscription_streams (subscription_name, stream_name, checkpoint, stream_position)
  select _subscription_name,
         stream_name,
         case when _start_from_beginning = true then 0 else stream_position end,
         stream_position
  from matching_streams
  on conflict (subscription_name, stream_name) do update
    set stream_position = excluded.stream_position;

  update subscriptions
  set initialized = true
  where name = _subscription_name;
end;
$$;

create function reset_subscription(
  _subscription_name text
)
  returns void
  language plpgsql
as
$$
begin
  if not exists(select from subscriptions where name = _subscription_name) then
    raise info 'Subscription not found: %', _subscription_name;

    return;
  end if;

  delete from subscription_streams where subscription_name = _subscription_name;

  update subscriptions
  set initialized = false
  where name = _subscription_name;
end;
$$;

create function delete_subscription(
  _subscription_name text
)
  returns void
  language plpgsql
as
$$
begin
  if not exists(select from subscriptions where name = _subscription_name) then
    raise info 'Subscription not found: %', _subscription_name;

    return;
  end if;

  delete from subscription_streams where subscription_name = _subscription_name;
  delete from subscriptions where name = _subscription_name;
end;
$$;

create function get_subscription_streams_to_process(
  _batch_size integer
)
  returns table(subscription_name text, stream_name text)
  language sql
as
$$
select subscription_name, stream_name
from subscription_streams
where checkpoint < stream_position
and blocked = false
limit _batch_size;
$$;

create function read_subscription_stream(
  _subscription_name text,
  _stream_name text,
  _batch_size integer
)
  returns setof stream_event
  language sql
as
$$
select e.id,
       e.stream_name,
       e.stream_position,
       e.global_position,
       e.type,
       e.data,
       e.metadata,
       e.timestamp
from events e
inner join subscription_streams ss on
  ss.subscription_name = _subscription_name and
  e.stream_name = ss.stream_name
where ss.blocked = false
and e.stream_name = _stream_name
and e.stream_position > ss.checkpoint
order by e.stream_position
limit _batch_size;
$$;

create function record_checkpoint(
  _subscription_name text,
  _stream_name text,
  _checkpoint bigint,
  _blocked boolean
)
  returns void
  language sql
as
$$
update subscription_streams
set checkpoint = _checkpoint,
    blocked = _blocked
where subscription_name = _subscription_name
and stream_name = _stream_name;
$$;
