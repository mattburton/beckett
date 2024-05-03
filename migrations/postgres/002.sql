alter type __schema__.new_stream_event add attribute deliver_at timestamp with time zone;

create table __schema__.scheduled_events
(
  id uuid not null primary key,
  stream_name text not null,
  type text not null,
  data jsonb not null,
  metadata jsonb not null,
  deliver_at timestamp with time zone not null,
  timestamp timestamp with time zone default now() not null
);

create index ix_scheduled_events_deliver_at on __schema__.scheduled_events (deliver_at);

create or replace function __schema__.append_to_stream(
  _stream_name text,
  _expected_version bigint,
  _events __schema__.new_stream_event[],
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
  perform pg_advisory_xact_lock(__schema__.stream_hash(_stream_name));

  select coalesce(max(e.stream_position), 0)
  into _current_version
  from __schema__.events e
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
    insert into __schema__.events (
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
    where e.deliver_at is null
    returning stream_position, type
  ),
  schedule_events as (
    insert into __schema__.scheduled_events (
      id,
      stream_name,
      type,
      data,
      metadata,
      deliver_at
    )
    select e.id,
           _stream_name,
           e.type,
           e.data,
           e.metadata,
           e.deliver_at
    from unnest(_events) as e
    where e.deliver_at is not null
  ),
  new_stream_version as (
    select max(stream_position) as stream_version
    from append_events
  ),
  record_subscription_streams as (
    insert into __schema__.subscription_streams (subscription_name, stream_name, stream_position)
    select s.name, _stream_name, v.stream_version
    from new_stream_version v, __schema__.subscriptions s
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

create function __schema__.deliver_scheduled_events(
  _send_poll_notification boolean default false
)
  returns void
  language sql
as
$$
with events_to_append as (
  delete from __schema__.scheduled_events
  where id in (
    select id
    from __schema__.scheduled_events
    where deliver_at <= current_timestamp
    for update
    skip locked
  )
  returning *
)
select __schema__.append_to_stream(
  e.stream_name,
  -2, --any stream version
  array[row(e.id, e.type, e.data, e.metadata, null)::__schema__.new_stream_event],
  _send_poll_notification
)
from events_to_append e;
$$;

create or replace function __schema__.record_checkpoint(
  _subscription_name text,
  _stream_name text,
  _checkpoint bigint,
  _blocked boolean
)
  returns void
  language plpgsql
as
$$
begin
  update __schema__.subscription_streams
  set checkpoint = _checkpoint,
      blocked = _blocked
  where subscription_name = _subscription_name
  and stream_name = _stream_name;

  if (_blocked = true) then
    perform __schema__.append_to_stream(
      '$retry-' || _subscription_name || '-' || _stream_name || '-' || _checkpoint::text,
      -2, --any stream version
      array[
        row(
          gen_random_uuid(),
          '$retry_created',
          json_build_object(
            'SubscriptionName', _subscription_name,
            'StreamName', _stream_name,
            'StreamPosition', _checkpoint,
            'Timestamp', current_timestamp
          ),
          '{}',
          null
        )::__schema__.new_stream_event
      ],
      true
    );
  end if;
end;
$$;
