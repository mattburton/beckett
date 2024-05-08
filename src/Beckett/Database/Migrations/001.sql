-------------------------------------------------
-- CREATE BECKETT ROLE
-------------------------------------------------

DO
$$
BEGIN
  IF EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'beckett') THEN
    RAISE NOTICE 'Role "beckett" already exists. Skipping.';
  ELSE
    CREATE ROLE beckett;
  END IF;
END
$$;

GRANT USAGE ON SCHEMA __schema__ to beckett;

-------------------------------------------------
-- EVENT STORE SUPPORT
-------------------------------------------------

CREATE TYPE __schema__.event AS
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
  id uuid NOT NULL UNIQUE,
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
  _events __schema__.event[]
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
  )
  SELECT max(stream_position) INTO _stream_version
  FROM append_events;

  PERFORM pg_notify('beckett:events', null);

  RETURN _stream_version;
END;
$$;

--TODO: return actual stream version regardless of filters
CREATE FUNCTION __schema__.read_stream(
  _stream_name text,
  _starting_stream_position bigint DEFAULT NULL,
  _ending_global_position bigint DEFAULT NULL,
  _count integer DEFAULT NULL,
  _read_forwards boolean DEFAULT true
)
  RETURNS TABLE (
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
AND (_starting_stream_position IS NULL OR e.stream_position >= _starting_stream_position)
AND (_ending_global_position IS NULL OR e.global_position <= _ending_global_position)
ORDER BY CASE WHEN _read_forwards = true THEN stream_position END,
         CASE WHEN _read_forwards = false THEN stream_position END DESC
LIMIT _count;
$$;

CREATE FUNCTION __schema__.read_stream_changes(
  _starting_global_position bigint,
  _batch_size int
)
  RETURNS TABLE (
    stream_name text,
    stream_version bigint,
    global_position bigint,
    event_types text[]
  )
  LANGUAGE sql
AS
$$
WITH last_transaction_id AS (
  SELECT transaction_id
  FROM __schema__.events
  WHERE global_position = _starting_global_position
  UNION ALL
  SELECT '0'::xid8 as transaction_id
  LIMIT 1
)
SELECT e.stream_name,
       max(e.stream_position) as stream_version,
       max(e.global_position) as global_position,
       array_agg(e.type) as event_types
FROM last_transaction_id lti, __schema__.events e
WHERE (
  (e.transaction_id = lti.transaction_id AND e.global_position > _starting_global_position)
  OR
  (e.transaction_id > lti.transaction_id)
)
AND e.transaction_id < pg_snapshot_xmin(pg_current_snapshot())
GROUP BY e.stream_name
LIMIT _batch_size;
$$;

-------------------------------------------------
-- SCHEDULED EVENTS SUPPORT
-------------------------------------------------

CREATE TYPE __schema__.scheduled_event AS
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
  _scheduled_events __schema__.scheduled_event[]
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

-------------------------------------------------
-- SUBSCRIPTION SUPPORT
-------------------------------------------------

CREATE TYPE __schema__.checkpoint AS
(
  name text,
  stream_name text,
  stream_version bigint
);

CREATE TABLE IF NOT EXISTS __schema__.subscriptions
(
  name text NOT NULL PRIMARY KEY,
  initialized boolean DEFAULT false NOT NULL
);

GRANT UPDATE, DELETE ON __schema__.subscriptions TO beckett;

CREATE TABLE IF NOT EXISTS __schema__.checkpoints
(
  stream_version bigint DEFAULT 0 NOT NULL,
  stream_position bigint DEFAULT 0 NOT NULL,
  blocked boolean DEFAULT false NOT NULL,
  name text NOT NULL,
  stream_name text NOT NULL,
  PRIMARY KEY (name, stream_name)
);

GRANT UPDATE, DELETE ON __schema__.checkpoints TO beckett;

CREATE FUNCTION __schema__.add_or_update_subscription(
  _name text
)
  RETURNS TABLE (
    initialized boolean
  )
  LANGUAGE sql
AS
$$
INSERT INTO __schema__.subscriptions (name)
VALUES (_name)
ON CONFLICT (name) DO NOTHING;

SELECT initialized
FROM __schema__.subscriptions
WHERE name = _name;
$$;

CREATE FUNCTION __schema__.get_next_uninitialized_subscription()
  RETURNS TABLE (
    name text
  )
  LANGUAGE sql
AS
$$
SELECT name
FROM __schema__.subscriptions
WHERE initialized = false
LIMIT 1;
$$;

CREATE FUNCTION __schema__.set_subscription_to_initialized(
  _name text
)
  RETURNS void
  LANGUAGE sql
AS
$$
DELETE FROM __schema__.checkpoints
WHERE name = _name
AND stream_name = '$initializing';

UPDATE __schema__.subscriptions
SET initialized = true
WHERE name = _name;
$$;

CREATE FUNCTION __schema__.lock_checkpoint(
  _name text,
  _stream_name text
)
  RETURNS TABLE (
    name text,
    stream_name text,
    stream_position bigint,
    stream_version bigint,
    blocked boolean
  )
  LANGUAGE sql
AS
$$
SELECT name, stream_name, stream_position, stream_version, blocked
FROM __schema__.checkpoints
WHERE name = _name
AND stream_name = _stream_name
FOR UPDATE
SKIP LOCKED;
$$;

CREATE FUNCTION __schema__.lock_next_available_checkpoint()
  RETURNS TABLE (
    name text,
    stream_name text,
    stream_position bigint,
    stream_version bigint,
    blocked boolean
  )
  LANGUAGE sql
AS
$$
SELECT c.name, c.stream_name, c.stream_position, c.stream_version, c.blocked
FROM __schema__.checkpoints c
INNER JOIN __schema__.subscriptions s on c.name = s.name
WHERE s.initialized = true
AND c.stream_position < c.stream_version
AND c.blocked = false
FOR UPDATE
SKIP LOCKED
LIMIT 1;
$$;

CREATE FUNCTION __schema__.record_checkpoints(
  _checkpoints __schema__.checkpoint[]
)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
BEGIN
  INSERT INTO __schema__.checkpoints (stream_version, name, stream_name)
  SELECT c.stream_version, c.name, c.stream_name
  FROM unnest(_checkpoints) c
  ON CONFLICT (name, stream_name) DO UPDATE
    SET stream_version = excluded.stream_version;

  PERFORM pg_notify('beckett:checkpoints', null);
END;
$$;

CREATE FUNCTION __schema__.update_checkpoint_stream_position(
  _name text,
  _stream_name text,
  _stream_position bigint,
  _blocked boolean
)
  RETURNS void
  LANGUAGE sql
AS
$$
UPDATE __schema__.checkpoints
SET stream_position = _stream_position,
    blocked = _blocked
WHERE name = _name
AND stream_name = _stream_name;
$$;

CREATE FUNCTION __schema__.record_checkpoint(
  _name text,
  _stream_name text,
  _stream_position bigint,
  _stream_version bigint
)
  RETURNS void
  LANGUAGE sql
AS
$$
INSERT INTO __schema__.checkpoints (stream_version, stream_position, name, stream_name)
VALUES (_stream_version, _stream_position, _name, _stream_name)
ON CONFLICT (name, stream_name) DO UPDATE
  SET stream_version = excluded.stream_version,
      stream_position = excluded.stream_position;
$$;

GRANT SELECT, INSERT ON ALL TABLES IN SCHEMA __schema__ TO beckett;
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA __schema__ TO beckett;

INSERT INTO __schema__.checkpoints (name, stream_name) VALUES ('$global', '$all');