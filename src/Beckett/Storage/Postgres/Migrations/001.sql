DO
$do$
BEGIN
   IF EXISTS (
      SELECT FROM pg_catalog.pg_roles
      WHERE  rolname = 'beckett') THEN

      RAISE NOTICE 'Role "beckett" already exists. Skipping.';
ELSE
CREATE ROLE beckett;
END IF;
END
$do$;

GRANT USAGE ON SCHEMA __schema__ to beckett;

-- EVENT STORE SUPPORT
CREATE TYPE __schema__.new_event AS
(
  id uuid,
  type text,
  data jsonb,
  metadata jsonb
);

CREATE OR REPLACE FUNCTION __schema__.stream_category(
  _stream_name text
)
  RETURNS text
  IMMUTABLE
  LANGUAGE sql
AS
$$
SELECT split_part(_stream_name, '-', 1);
$$;

CREATE TABLE IF NOT EXISTS __schema__.events
(
  id uuid not null UNIQUE,
  global_position bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  stream_position bigint NOT NULL,
  transaction_id xid8 DEFAULT pg_current_xact_id() NOT NULL,
  timestamp timestamp with time zone DEFAULT now() NOT NULL,
  stream_name text NOT NULL,
  type text NOT NULL,
  data jsonb NOT NULL,
  metadata jsonb NOT NULL,
  UNIQUE (stream_name, stream_position)
);

CREATE INDEX IF NOT EXISTS ix_events_type ON __schema__.events (type);

CREATE INDEX IF NOT EXISTS ix_events_stream_category ON __schema__.events (
  __schema__.stream_category(stream_name)
);

CREATE FUNCTION __schema__.stream_hash(
  _stream_name text
)
  RETURNS bigint
  IMMUTABLE
  LANGUAGE sql
AS
$$
SELECT abs(hashtextextended(_stream_name, 0));
$$;

CREATE OR REPLACE FUNCTION __schema__.append_to_stream(
  _stream_name text,
  _expected_version bigint,
  _events __schema__.new_event[]
)
  RETURNS bigint
  LANGUAGE plpgsql
AS
$$
DECLARE
  _current_version bigint;
  _stream_version bigint;
BEGIN
  PERFORM pg_advisory_xact_lock(__schema__.stream_hash(_stream_name));

  SELECT coalesce(max(e.stream_position), 0)
  INTO _current_version
  FROM __schema__.events e
  WHERE e.stream_name = _stream_name;

  IF (_expected_version < -2) THEN
    RAISE EXCEPTION 'Invalid value for expected version: %', _expected_version;
  END IF;

  IF (_expected_version = -1 AND _current_version = 0) THEN
    RAISE EXCEPTION 'Attempted to append to a non-existing stream: %', _stream_name;
  END IF;

  IF (_expected_version = 0 AND _current_version > 0) THEN
    RAISE EXCEPTION 'Attempted to start a stream that already exists: %', _stream_name;
  END IF;

  IF (_expected_version > 0 AND _expected_version != _current_version) THEN
    RAISE EXCEPTION 'Stream % version % does not match expected version %',
      _stream_name,
      _current_version,
      _expected_version;
  END IF;

  WITH append_events AS (
    INSERT INTO __schema__.events (
      id,
      stream_position,
      stream_name,
      type,
      data,
      metadata
    )
    SELECT e.id,
           _current_version + (row_number() over())::bigint,
           _stream_name,
           e.type,
           e.data,
           e.metadata
    FROM unnest(_events) AS e
    RETURNING stream_position, type
  ),
  new_stream_version AS (
    SELECT max(stream_position) AS stream_version
    FROM append_events
  ),
  record_subscription_streams AS (
    INSERT INTO __schema__.checkpoints (subscription_name, stream_name, stream_version)
    SELECT s.name, _stream_name, v.stream_version
    FROM new_stream_version v, __schema__.subscriptions s
    INNER JOIN append_events e on e.type = ANY (s.event_types)
    ON CONFLICT (subscription_name, stream_name) DO UPDATE
      SET stream_version = excluded.stream_version
  )
  SELECT stream_version INTO _stream_version
  FROM new_stream_version;

  PERFORM pg_notify('beckett:poll', null);

  RETURN _stream_version;
END;
$$;

--TODO: return actual stream version regardless of filters
CREATE FUNCTION __schema__.read_stream(
  _stream_name text,
  _starting_stream_position bigint DEFAULT null,
  _ending_global_position bigint DEFAULT null,
  _count integer DEFAULT null,
  _read_forwards boolean DEFAULT true
)
  RETURNS table (
    id uuid,
    stream_name text,
    stream_position bigint,
    global_position bigint,
    type text,
    data text,
    metadata text,
    "timestamp" timestamp with time zone
  )
  LANGUAGE sql
AS
$$
SELECT e.id,
       e.stream_name,
       e.stream_position,
       e.global_position,
       e.type,
       e.data,
       e.metadata,
       e.timestamp
FROM __schema__.events e
WHERE e.stream_name = _stream_name
AND (_starting_stream_position IS NULL or e.stream_position >= _starting_stream_position)
AND (_ending_global_position IS NULL or e.global_position <= _ending_global_position)
ORDER BY CASE WHEN _read_forwards = true THEN stream_position END,
         CASE WHEN _read_forwards = false THEN stream_position END DESC
LIMIT _count;
$$;

-- SCHEDULED EVENTS SUPPORT
CREATE TYPE __schema__.new_scheduled_event AS
(
  id uuid,
  type text,
  data jsonb,
  metadata jsonb,
  deliver_at timestamp with time zone
);

CREATE TABLE __schema__.scheduled_events
(
  id uuid NOT NULL PRIMARY KEY,
  stream_name text NOT NULL,
  type text NOT NULL,
  data jsonb NOT NULL,
  metadata jsonb NOT NULL,
  deliver_at timestamp with time zone NOT NULL,
  timestamp timestamp with time zone DEFAULT now() NOT NULL
);

GRANT UPDATE, DELETE ON __schema__.scheduled_events TO beckett;

CREATE OR REPLACE FUNCTION __schema__.schedule_events(
  _stream_name text,
  _scheduled_events __schema__.new_scheduled_event[]
)
  RETURNS void
  LANGUAGE sql
AS
$$
INSERT INTO __schema__.scheduled_events (
  id,
  stream_name,
  type,
  data,
  metadata,
  deliver_at
)
SELECT e.id, _stream_name, e.type, e.data, e.metadata, e.deliver_at
FROM unnest(_scheduled_events) as e
ON CONFLICT (id) DO NOTHING;
$$;

CREATE OR REPLACE FUNCTION __schema__.get_scheduled_events_to_deliver(
  _batch_size int
)
  RETURNS setof __schema__.scheduled_events
  LANGUAGE sql
AS
$$
DELETE FROM __schema__.scheduled_events
WHERE id IN (
  SELECT id
  FROM __schema__.scheduled_events
  WHERE deliver_at <= CURRENT_TIMESTAMP
  FOR UPDATE
  SKIP LOCKED
  LIMIT _batch_size
)
RETURNING *;
$$;

-- SUBSCRIPTION SUPPORT
CREATE TABLE IF NOT EXISTS __schema__.subscriptions
(
  name text not null PRIMARY KEY,
  event_types text[] NOT NULL,
  initialized boolean DEFAULT false NOT NULL
);

GRANT UPDATE, DELETE ON __schema__.subscriptions TO beckett;

CREATE TABLE IF NOT EXISTS __schema__.checkpoints
(
  stream_version bigint DEFAULT 0 NOT NULL,
  stream_position bigint DEFAULT 0 NOT NULL,
  transaction_id xid8 DEFAULT pg_current_xact_id() NOT NULL,
  blocked boolean DEFAULT false NOT NULL,
  subscription_name text NOT NULL,
  stream_name text NOT NULL,
  PRIMARY KEY (subscription_name, stream_name)
);

GRANT UPDATE, DELETE ON __schema__.checkpoints TO beckett;

CREATE FUNCTION __schema__.add_or_update_subscription(
  _subscription_name text,
  _event_types text[],
  _start_from_beginning boolean
)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
DECLARE
  _initialized boolean;
BEGIN
  INSERT INTO __schema__.subscriptions (name, event_types)
  values (_subscription_name, _event_types)
  ON CONFLICT (name) DO UPDATE SET event_types = excluded.event_types
  RETURNING initialized INTO _initialized;

  IF (_initialized = true) THEN
    RETURN;
  END IF;

  WITH matching_streams AS (
    SELECT stream_name, max(stream_position) AS stream_position
    FROM __schema__.events
    WHERE type = any(_event_types)
    group by stream_name
  )
  INSERT INTO __schema__.checkpoints (subscription_name, stream_name, stream_position, stream_version)
  SELECT _subscription_name,
         stream_name,
         CASE WHEN _start_from_beginning = true THEN 0 else stream_position END,
         stream_position
  FROM matching_streams
  ON CONFLICT (subscription_name, stream_name) DO UPDATE
    SET stream_version = excluded.stream_version;

  UPDATE __schema__.subscriptions
  SET initialized = true
  WHERE name = _subscription_name;
END;
$$;

CREATE FUNCTION __schema__.get_subscription_streams_to_process(
  _batch_size integer
)
  RETURNS table(subscription_name text, stream_name text)
  LANGUAGE sql
AS
$$
SELECT subscription_name, stream_name
FROM __schema__.checkpoints
WHERE stream_position < stream_version
AND blocked = false
LIMIT _batch_size;
$$;

CREATE FUNCTION __schema__.read_subscription_stream(
  _subscription_name text,
  _stream_name text,
  _batch_size integer
)
  RETURNS table (
    id uuid,
    stream_name text,
    stream_position bigint,
    global_position bigint,
    type text,
    data text,
    metadata text,
    "timestamp" timestamp with time zone
  )
  LANGUAGE sql
AS
$$
SELECT e.id,
       e.stream_name,
       e.stream_position,
       e.global_position,
       e.type,
       e.data,
       e.metadata,
       e.timestamp
FROM __schema__.events e
INNER JOIN __schema__.checkpoints ss on
  ss.subscription_name = _subscription_name AND
  e.stream_name = ss.stream_name
WHERE ss.blocked = false
AND e.stream_name = _stream_name
AND e.stream_position > ss.stream_position
ORDER BY e.stream_position
LIMIT _batch_size;
$$;

CREATE FUNCTION __schema__.record_checkpoint(
  _subscription_name text,
  _stream_name text,
  _position bigint,
  _blocked boolean
)
  RETURNS void
  LANGUAGE sql
AS
$$
UPDATE __schema__.checkpoints
SET stream_position = _position,
    blocked = _blocked
WHERE subscription_name = _subscription_name
AND stream_name = _stream_name;
$$;

CREATE FUNCTION __schema__.unblock_checkpoint(
  _subscription_name text,
  _stream_name text
)
  RETURNS void
  LANGUAGE sql
AS
$$
UPDATE __schema__.checkpoints
SET blocked = false
WHERE subscription_name = _subscription_name
AND stream_name = _stream_name;
$$;


GRANT SELECT, INSERT ON ALL TABLES IN SCHEMA __schema__ TO beckett;
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA __schema__ TO beckett;
