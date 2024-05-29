-------------------------------------------------
-- CREATE BECKETT ROLE
-------------------------------------------------

DO
$$
BEGIN
  IF EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'beckett') THEN
    RAISE NOTICE 'Role "beckett" already exists - skipping';
  ELSE
    CREATE ROLE beckett;
  END IF;
END
$$;

GRANT USAGE ON SCHEMA __schema__ to beckett;
ALTER DEFAULT PRIVILEGES IN SCHEMA __schema__ GRANT SELECT, INSERT ON TABLES TO beckett;
ALTER DEFAULT PRIVILEGES IN SCHEMA __schema__ GRANT EXECUTE ON FUNCTIONS TO beckett;

-------------------------------------------------
-- MESSAGE STORE SUPPORT
-------------------------------------------------

CREATE TYPE __schema__.message AS
(
  id uuid,
  stream_name text,
  type text,
  data jsonb,
  metadata jsonb,
  expected_version bigint
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

CREATE TABLE IF NOT EXISTS __schema__.messages
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

CREATE INDEX IF NOT EXISTS ix_messages_stream_category ON __schema__.messages (
  __schema__.stream_category(stream_name)
);

CREATE OR REPLACE FUNCTION __schema__.stream_hash(
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
  _messages __schema__.message[]
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

  SELECT coalesce(max(m.stream_position), 0)
  INTO _current_version
  FROM __schema__.messages m
  WHERE m.stream_name = _stream_name;

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

  WITH append_messages AS (
    INSERT INTO __schema__.messages (
      id,
      stream_position,
      stream_name,
      type,
      data,
      metadata
    )
    SELECT m.id,
           _current_version + (row_number() over())::bigint,
           _stream_name,
           m.type,
           m.data,
           m.metadata
    FROM unnest(_messages) AS m
    RETURNING stream_position, type
  )
  SELECT max(stream_position) INTO _stream_version
  FROM append_messages;

  PERFORM pg_notify('beckett:messages', NULL);

  RETURN _stream_version;
END;
$$;

CREATE OR REPLACE FUNCTION __schema__.append_message(
  _id uuid,
  _stream_name text,
  _type text,
  _data jsonb,
  _metadata jsonb,
  _expected_version bigint
)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
DECLARE
  _current_version bigint;
  _stream_position bigint;
BEGIN
  PERFORM pg_advisory_xact_lock(__schema__.stream_hash(_stream_name));

  SELECT coalesce(max(m.stream_position), 0)
  INTO _current_version
  FROM __schema__.messages m
  WHERE m.stream_name = _stream_name;

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

  _stream_position := _current_version + 1;

  INSERT INTO __schema__.messages (
    id,
    stream_position,
    stream_name,
    type,
    data,
    metadata
  ) VALUES (
    _id,
    _stream_position,
    _stream_name,
    _type,
    _data,
    _metadata
  );
END;
$$;

CREATE OR REPLACE FUNCTION __schema__.append_messages(
  _messages __schema__.message[]
)
  RETURNS void
  LANGUAGE sql
AS
$$
SELECT __schema__.append_message(
  m.id,
  m.stream_name,
  m.type,
  m.data,
  m.metadata,
  m.expected_version
)
FROM unnest(_messages) AS m;

SELECT pg_notify('beckett:messages', NULL);
$$;

CREATE OR REPLACE FUNCTION __schema__.read_stream(
  _stream_name text,
  _starting_stream_position bigint DEFAULT NULL,
  _ending_global_position bigint DEFAULT NULL,
  _count integer DEFAULT NULL,
  _read_forwards boolean DEFAULT true
)
  RETURNS TABLE (
    id uuid,
    stream_name text,
    stream_version bigint,
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
WITH stream_version AS (
  SELECT max(m.stream_position) as stream_version
  FROM __schema__.messages m
  WHERE m.stream_name = _stream_name
)
SELECT m.id,
       m.stream_name,
       sv.stream_version,
       m.stream_position,
       m.global_position,
       m.type,
       m.data,
       m.metadata,
       m.timestamp
FROM stream_version sv, __schema__.messages m
WHERE m.stream_name = _stream_name
AND (_starting_stream_position IS NULL OR m.stream_position >= _starting_stream_position)
AND (_ending_global_position IS NULL OR m.global_position <= _ending_global_position)
ORDER BY CASE WHEN _read_forwards = true THEN stream_position END,
         CASE WHEN _read_forwards = false THEN stream_position END DESC
LIMIT _count;
$$;

CREATE OR REPLACE FUNCTION __schema__.read_stream_changes(
  _starting_global_position bigint,
  _batch_size int
)
  RETURNS TABLE (
    stream_name text,
    stream_version bigint,
    global_position bigint,
    message_types text[]
  )
  LANGUAGE sql
AS
$$
WITH last_transaction_id AS (
  SELECT transaction_id
  FROM __schema__.messages
  WHERE global_position = _starting_global_position
  UNION ALL
  SELECT '0'::xid8 as transaction_id
  LIMIT 1
)
SELECT m.stream_name,
       max(m.stream_position) as stream_version,
       max(m.global_position) as global_position,
       array_agg(m.type) as message_types
FROM last_transaction_id lti, __schema__.messages m
WHERE (
  (m.transaction_id = lti.transaction_id AND m.global_position > _starting_global_position)
  OR
  (m.transaction_id > lti.transaction_id)
)
AND m.transaction_id < pg_snapshot_xmin(pg_current_snapshot())
GROUP BY m.stream_name
LIMIT _batch_size;
$$;

-------------------------------------------------
-- SCHEDULED MESSAGE SUPPORT
-------------------------------------------------

CREATE TYPE __schema__.scheduled_message AS
(
  id uuid,
  type text,
  data jsonb,
  metadata jsonb,
  deliver_at timestamp with time zone
);

CREATE TABLE __schema__.scheduled_messages
(
  id uuid NOT NULL PRIMARY KEY,
  stream_name text NOT NULL,
  type text NOT NULL,
  data jsonb NOT NULL,
  metadata jsonb NOT NULL,
  deliver_at timestamp with time zone NOT NULL,
  timestamp timestamp with time zone DEFAULT now() NOT NULL
);

GRANT UPDATE, DELETE ON __schema__.scheduled_messages TO beckett;

CREATE OR REPLACE FUNCTION __schema__.schedule_message(
  _stream_name text,
  _scheduled_message __schema__.scheduled_message
)
  RETURNS void
  LANGUAGE sql
AS
$$
INSERT INTO __schema__.scheduled_messages (
  id,
  stream_name,
  type,
  data,
  metadata,
  deliver_at
)
VALUES (
  _scheduled_message.id,
  _stream_name,
  _scheduled_message.type,
  _scheduled_message.data,
  _scheduled_message.metadata,
  _scheduled_message.deliver_at
)
ON CONFLICT (id) DO NOTHING;
$$;

CREATE OR REPLACE FUNCTION __schema__.get_scheduled_messages_to_deliver(
  _batch_size int
)
  RETURNS setof __schema__.scheduled_messages
  LANGUAGE sql
AS
$$
DELETE FROM __schema__.scheduled_messages
WHERE id IN (
  SELECT id
  FROM __schema__.scheduled_messages
  WHERE deliver_at <= CURRENT_TIMESTAMP
  FOR UPDATE
  SKIP LOCKED
  LIMIT _batch_size
)
RETURNING *;
$$;

CREATE OR REPLACE FUNCTION __schema__.cancel_scheduled_message(
  _id uuid
)
  RETURNS void
  LANGUAGE sql
AS
$$
DELETE FROM __schema__.scheduled_messages WHERE id = _id;
$$;

-------------------------------------------------
-- SUBSCRIPTION SUPPORT
-------------------------------------------------

CREATE TYPE __schema__.checkpoint AS
(
  application text,
  name text,
  stream_name text,
  stream_version bigint
);

CREATE TYPE __schema__.checkpoint_status AS ENUM ('active', 'retry', 'failed');

CREATE TABLE IF NOT EXISTS __schema__.subscriptions
(
  application text NOT NULL,
  name text NOT NULL,
  initialized boolean DEFAULT false NOT NULL,
  PRIMARY KEY (application, name)
);

GRANT UPDATE, DELETE ON __schema__.subscriptions TO beckett;

CREATE TABLE IF NOT EXISTS __schema__.checkpoints
(
  stream_version bigint DEFAULT 0 NOT NULL,
  stream_position bigint DEFAULT 0 NOT NULL,
  application text NOT NULL,
  name text NOT NULL,
  stream_name text NOT NULL,
  status __schema__.checkpoint_status DEFAULT 'active' NOT NULL,
  retry_id uuid NULL,
  PRIMARY KEY (application, name, stream_name)
);

GRANT UPDATE, DELETE ON __schema__.checkpoints TO beckett;

CREATE OR REPLACE FUNCTION __schema__.add_or_update_subscription(
  _application text,
  _name text
)
  RETURNS TABLE (
    initialized boolean
  )
  LANGUAGE sql
AS
$$
INSERT INTO __schema__.subscriptions (application, name)
VALUES (_application, _name)
ON CONFLICT (application, name) DO NOTHING;

SELECT initialized
FROM __schema__.subscriptions
WHERE name = _name;
$$;

CREATE OR REPLACE FUNCTION __schema__.get_next_uninitialized_subscription(
  _application text
)
  RETURNS TABLE (
    name text
  )
  LANGUAGE sql
AS
$$
SELECT name
FROM __schema__.subscriptions
WHERE application = _application
AND initialized = false
LIMIT 1;
$$;

CREATE OR REPLACE FUNCTION __schema__.set_subscription_to_initialized(
  _application text,
  _name text
)
  RETURNS void
  LANGUAGE sql
AS
$$
DELETE FROM __schema__.checkpoints
WHERE application = _application
AND name = _name
AND stream_name = '$initializing';

UPDATE __schema__.subscriptions
SET initialized = true
WHERE application = _application
AND name = _name;
$$;

CREATE OR REPLACE FUNCTION __schema__.ensure_checkpoint_exists(
  _application text,
  _name text,
  _stream_name text
)
  RETURNS void
  LANGUAGE sql
AS
$$
INSERT INTO __schema__.checkpoints (application, name, stream_name)
VALUES (_application, _name, _stream_name)
ON CONFLICT (application, name, stream_name) DO NOTHING;
$$;

CREATE OR REPLACE FUNCTION __schema__.lock_checkpoint(
  _application text,
  _name text,
  _stream_name text
)
  RETURNS TABLE (
    application text,
    name text,
    stream_name text,
    stream_position bigint,
    stream_version bigint,
    status __schema__.checkpoint_status
  )
  LANGUAGE sql
AS
$$
SELECT application, name, checkpoints.stream_name, stream_position, stream_version, status
FROM __schema__.checkpoints
WHERE application = _application
AND name = _name
AND stream_name = _stream_name
FOR UPDATE
SKIP LOCKED;
$$;

CREATE OR REPLACE FUNCTION beckett.checkpoint_hash(
  _application text,
  _name text,
  _stream_name text
)
  RETURNS bigint
  IMMUTABLE
  LANGUAGE sql
AS
$$
SELECT abs(hashtextextended(_application || '-' || _name || '-' || _stream_name, 0));
$$;

CREATE OR REPLACE FUNCTION __schema__.lock_next_available_checkpoint(
  _application text
)
  RETURNS TABLE (
    application text,
    name text,
    stream_name text,
    stream_position bigint,
    stream_version bigint,
    status __schema__.checkpoint_status
  )
  LANGUAGE sql
AS
$$
WITH available_checkpoints AS (
    SELECT c.application,
           c.name,
           c.stream_name,
           c.stream_position,
           c.stream_version,
           c.status
    FROM __schema__.checkpoints c
    INNER JOIN __schema__.subscriptions s
    ON c.application = s.application AND c.name = s.name
    WHERE s.application = _application
    AND s.initialized = true
    AND c.stream_position < c.stream_version
    AND c.status = 'active'
    LIMIT 5
),
lock_checkpoint AS (
    SELECT application,
           name,
           stream_name,
           stream_position,
           stream_version,
           status
    FROM available_checkpoints
    WHERE pg_try_advisory_xact_lock(beckett.checkpoint_hash(application, name, stream_name)) = true
    LIMIT 1
)
SELECT application,
       name,
       stream_name,
       stream_position,
       stream_version,
       status
FROM lock_checkpoint;
$$;

CREATE OR REPLACE FUNCTION __schema__.record_checkpoints(
  _checkpoints __schema__.checkpoint[]
)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
BEGIN
  INSERT INTO __schema__.checkpoints (stream_version, application, name, stream_name)
  SELECT c.stream_version, c.application, c.name, c.stream_name
  FROM unnest(_checkpoints) c
  ON CONFLICT (application, name, stream_name) DO UPDATE
    SET stream_version = excluded.stream_version;

  PERFORM pg_notify('beckett:checkpoints', NULL);
END;
$$;

CREATE OR REPLACE FUNCTION __schema__.update_checkpoint_status(
  _application text,
  _name text,
  _stream_name text,
  _stream_position bigint,
  _status __schema__.checkpoint_status
)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
BEGIN
  UPDATE __schema__.checkpoints
  SET stream_position = _stream_position,
      status = _status,
      retry_id = NULL
  WHERE application = _application
  AND name = _name
  AND stream_name = _stream_name;

  IF (_status = 'retry') THEN
    PERFORM pg_notify('beckett:retries', NULL);
  END IF;
END;
$$;

CREATE OR REPLACE FUNCTION __schema__.record_checkpoint(
  _application text,
  _name text,
  _stream_name text,
  _stream_position bigint,
  _stream_version bigint
)
  RETURNS void
  LANGUAGE sql
AS
$$
INSERT INTO __schema__.checkpoints (stream_version, stream_position, application, name, stream_name)
VALUES (_stream_version, _stream_position, _application, _name, _stream_name)
ON CONFLICT (application, name, stream_name) DO UPDATE
  SET stream_version = excluded.stream_version,
      stream_position = excluded.stream_position;
$$;

CREATE OR REPLACE FUNCTION __schema__.lock_next_checkpoint_for_retry(
  _application text
)
  RETURNS TABLE (
    application text,
    name text,
    stream_name text,
    stream_position bigint,
    retry_id uuid
  )
  LANGUAGE sql
AS
$$
WITH retry AS (
  SELECT c.application,
         c.name,
         c.stream_name
  FROM __schema__.checkpoints c
  WHERE c.application = _application
  AND c.status = 'retry'
  AND c.retry_id IS NULL
  FOR UPDATE
  SKIP LOCKED
  LIMIT 1
)
UPDATE __schema__.checkpoints AS c
SET retry_id = gen_random_uuid()
FROM retry AS r
WHERE c.application = r.application
AND c.name = r.name
AND c.stream_name = r.stream_name
RETURNING c.application, c.name, c.stream_name, c.stream_position, c.retry_id;
$$;

CREATE OR REPLACE FUNCTION __schema__.get_subscription_lag(
  _application text
)
  RETURNS bigint
  LANGUAGE sql
AS
$$
SELECT count(*)
FROM __schema__.checkpoints
WHERE application = _application
AND starts_with(name, '$') = false
AND stream_position < stream_version
AND status = 'active';
$$;

CREATE OR REPLACE FUNCTION __schema__.get_subscription_retry_count(
  _application text
)
  RETURNS bigint
  LANGUAGE sql
AS
$$
SELECT count(*)
FROM __schema__.checkpoints
WHERE application = _application
AND status = 'retry';
$$;

CREATE OR REPLACE FUNCTION __schema__.get_subscription_failed_count(
  _application text
)
  RETURNS bigint
  LANGUAGE sql
AS
$$
SELECT count(*)
FROM __schema__.checkpoints
WHERE application = _application
AND status = 'failed';
$$;
