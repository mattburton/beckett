-------------------------------------------------
-- CREATE BECKETT SCHEMA / ROLE
-------------------------------------------------
-- noinspection SqlResolveForFile

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

CREATE SCHEMA IF NOT EXISTS __schema__;

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
  id uuid NOT NULL,
  global_position bigint GENERATED ALWAYS AS IDENTITY,
  stream_position bigint NOT NULL,
  transaction_id xid8 DEFAULT pg_current_xact_id() NOT NULL,
  timestamp timestamp with time zone DEFAULT now() NOT NULL,
  archived boolean NOT NULL DEFAULT false,
  stream_name text NOT NULL,
  type text NOT NULL,
  data jsonb NOT NULL,
  metadata jsonb NOT NULL,
  PRIMARY KEY (global_position, archived),
  UNIQUE (id, archived),
  UNIQUE (stream_name, stream_position, archived)
) PARTITION BY LIST (archived);

GRANT UPDATE ON __schema__.messages TO beckett;

CREATE TABLE IF NOT EXISTS __schema__.messages_active PARTITION OF __schema__.messages FOR VALUES IN (false);

CREATE TABLE IF NOT EXISTS __schema__.messages_archived PARTITION OF __schema__.messages FOR VALUES IN (true);

CREATE INDEX IF NOT EXISTS ix_messages_active_global_read_stream ON __schema__.messages_active (transaction_id, global_position, archived);

CREATE INDEX IF NOT EXISTS ix_messages_active_tenant_stream_category on __schema__.messages_active ((metadata ->> '$tenant'), beckett.stream_category(stream_name))
  WHERE metadata ->> '$tenant' IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_messages_active_correlation_id ON __schema__.messages_active ((metadata ->> '$correlation_id'))
  WHERE metadata ->> '$correlation_id' IS NOT NULL;

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
  WHERE m.stream_name = _stream_name
  AND m.archived = false;

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

CREATE OR REPLACE FUNCTION __schema__.read_stream(
  _stream_name text,
  _starting_stream_position bigint DEFAULT NULL,
  _ending_stream_position bigint DEFAULT NULL,
  _starting_global_position bigint DEFAULT NULL,
  _ending_global_position bigint DEFAULT NULL,
  _count integer DEFAULT NULL,
  _read_forwards boolean DEFAULT true,
  _types text[] DEFAULT NULL
)
  RETURNS TABLE (
    id uuid,
    stream_name text,
    stream_version bigint,
    stream_position bigint,
    global_position bigint,
    type text,
    data jsonb,
    metadata jsonb,
    "timestamp" timestamp with time zone
  )
  LANGUAGE plpgsql
AS
$$
DECLARE
  _stream_version bigint;
BEGIN
  SELECT max(m.stream_position)
  INTO _stream_version
  FROM __schema__.messages m
  WHERE m.stream_name = _stream_name
  AND m.archived = false;

  IF (_stream_version IS NULL) THEN
    _stream_version = 0;
  END IF;

  RETURN QUERY
    SELECT m.id,
           m.stream_name,
           _stream_version as stream_version,
           m.stream_position,
           m.global_position,
           m.type,
           m.data,
           m.metadata,
           m.timestamp
    FROM __schema__.messages m
    WHERE m.stream_name = _stream_name
    AND (_starting_stream_position IS NULL OR m.stream_position >= _starting_stream_position)
    AND (_ending_stream_position IS NULL OR m.stream_position <= _ending_stream_position)
    AND m.archived = false
    AND (_starting_global_position IS NULL OR m.global_position >= _starting_global_position)
    AND (_ending_global_position IS NULL OR m.global_position <= _ending_global_position)
    AND (_types IS NULL OR m.type = ANY(_types))
    ORDER BY CASE WHEN _read_forwards = true THEN m.stream_position END,
             CASE WHEN _read_forwards = false THEN m.stream_position END DESC
    LIMIT _count;
END;
$$;

CREATE OR REPLACE FUNCTION __schema__.read_index_batch(
  _starting_global_position bigint,
  _batch_size int
)
  RETURNS TABLE (
    stream_name text,
    stream_position bigint,
    global_position bigint,
    type text,
    tenant text,
    "timestamp" timestamp with time zone
  )
  LANGUAGE plpgsql
AS
$$
DECLARE
  _transaction_id xid8;
BEGIN
  SELECT m.transaction_id
  INTO _transaction_id
  FROM __schema__.messages m
  WHERE m.global_position = _starting_global_position
  AND m.archived = false;

  IF (_transaction_id IS NULL) THEN
    _transaction_id = '0'::xid8;
  END IF;

  RETURN QUERY
    SELECT m.stream_name,
           m.stream_position,
           m.global_position,
           m.type,
           m.metadata ->> '$tenant',
           m.timestamp
    FROM __schema__.messages m
    WHERE (m.transaction_id, m.global_position) > (_transaction_id, _starting_global_position)
    AND m.transaction_id < pg_snapshot_xmin(pg_current_snapshot())
    AND m.archived = false
    ORDER BY m.transaction_id, m.global_position
    LIMIT _batch_size;
END;
$$;

CREATE OR REPLACE FUNCTION __schema__.read_global_stream(
  _starting_global_position bigint,
  _count int,
  _category text DEFAULT NULL,
  _types text[] DEFAULT NULL
)
  RETURNS TABLE (
    id uuid,
    stream_name text,
    stream_position bigint,
    global_position bigint,
    type text,
    data jsonb,
    metadata jsonb,
    "timestamp" timestamp with time zone
  )
  LANGUAGE plpgsql
AS
$$
DECLARE
  _transaction_id xid8;
  _ending_global_position bigint;
BEGIN
  SELECT m.transaction_id
  INTO _transaction_id
  FROM __schema__.messages m
  WHERE m.global_position = _starting_global_position
  AND m.archived = false;

  IF (_transaction_id IS NULL) THEN
    _transaction_id = '0'::xid8;
  END IF;

  _ending_global_position = _starting_global_position + _count;

  RETURN QUERY
    SELECT m.id,
           m.stream_name,
           m.stream_position,
           m.global_position,
           m.type,
           m.data,
           m.metadata,
           m.timestamp
    FROM __schema__.messages m
    WHERE (m.transaction_id, m.global_position) > (_transaction_id, _starting_global_position)
    AND (m.global_position > _starting_global_position AND m.global_position <= _ending_global_position)
    AND m.transaction_id < pg_snapshot_xmin(pg_current_snapshot())
    AND m.archived = false
    AND (_category IS NULL OR __schema__.stream_category(m.stream_name) = _category)
    AND (_types IS NULL OR m.type = ANY(_types))
    ORDER BY m.transaction_id, m.global_position;
END;
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

CREATE INDEX ix_scheduled_messages_deliver_at ON __schema__.scheduled_messages (deliver_at ASC);

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
  group_name text,
  name text,
  stream_name text,
  stream_version bigint,
  stream_position bigint
);

CREATE TYPE __schema__.retry AS
(
  attempt int,
  error jsonb,
  timestamp timestamp with time zone
);

CREATE TYPE __schema__.subscription_status AS ENUM (
  'uninitialized',
  'active',
  'paused',
  'unknown'
);

CREATE TYPE __schema__.checkpoint_status AS ENUM (
  'active',
  'retry',
  'failed'
);

CREATE TABLE IF NOT EXISTS __schema__.subscriptions
(
  group_name text NOT NULL,
  name text NOT NULL,
  status __schema__.subscription_status DEFAULT 'uninitialized' NOT NULL,
  PRIMARY KEY (group_name, name)
);

CREATE INDEX ix_subscriptions_active ON __schema__.subscriptions (group_name, name, status) WHERE status = 'active';

GRANT UPDATE, DELETE ON __schema__.subscriptions TO beckett;

CREATE TABLE IF NOT EXISTS __schema__.checkpoints
(
  id bigint PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
  stream_version bigint NOT NULL DEFAULT 0,
  stream_position bigint NOT NULL DEFAULT 0,
  created_at timestamp with time zone DEFAULT now(),
  updated_at timestamp with time zone DEFAULT now(),
  process_at timestamp with time zone NULL,
  reserved_until timestamp with time zone NULL,
  lagging boolean GENERATED ALWAYS AS (stream_version > stream_position) STORED,
  status __schema__.checkpoint_status NOT NULL DEFAULT 'active',
  group_name text NOT NULL,
  name text NOT NULL,
  stream_name text NOT NULL,
  retries __schema__.retry[] NULL,
  UNIQUE (group_name, name, stream_name)
);

CREATE INDEX IF NOT EXISTS ix_checkpoints_to_process ON __schema__.checkpoints (group_name, process_at, reserved_until)
  WHERE process_at IS NOT NULL AND reserved_until IS NULL;

CREATE INDEX IF NOT EXISTS ix_checkpoints_reserved ON __schema__.checkpoints (group_name, reserved_until)
  WHERE reserved_until IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_checkpoints_metrics ON __schema__.checkpoints (status, lagging, group_name, name);

CREATE FUNCTION __schema__.checkpoint_preprocessor()
  RETURNS trigger
  LANGUAGE plpgsql
AS
$$
BEGIN
  IF (TG_OP = 'UPDATE') THEN
    NEW.updated_at = now();
  END IF;

  IF (NEW.status = 'active' AND NEW.process_at IS NULL AND NEW.stream_version > NEW.stream_position) THEN
    NEW.process_at = now();
  END IF;

  IF (NEW.name != '$global' AND NEW.process_at IS NOT NULL AND NEW.reserved_until IS NULL) THEN
    PERFORM pg_notify('beckett:checkpoints', NEW.group_name);
  END IF;

  RETURN NEW;
END;
$$;

CREATE TRIGGER checkpoint_preprocessor BEFORE INSERT OR UPDATE ON __schema__.checkpoints
  FOR EACH ROW EXECUTE FUNCTION __schema__.checkpoint_preprocessor();

GRANT UPDATE, DELETE ON __schema__.checkpoints TO beckett;

-------------------------------------------------
-- SUBSCRIPTIONS
-------------------------------------------------

CREATE OR REPLACE FUNCTION __schema__.add_or_update_subscription(
  _group_name text,
  _name text
)
  RETURNS TABLE (
    status __schema__.subscription_status
  )
  LANGUAGE sql
AS
$$
INSERT INTO __schema__.subscriptions (group_name, name)
VALUES (_group_name, _name)
ON CONFLICT (group_name, name) DO NOTHING;

SELECT status
FROM __schema__.subscriptions
WHERE group_name = _group_name
AND name = _name;
$$;

CREATE OR REPLACE FUNCTION __schema__.get_next_uninitialized_subscription(
  _group_name text
)
  RETURNS TABLE (
    name text
  )
  LANGUAGE sql
AS
$$
SELECT name
FROM __schema__.subscriptions
WHERE group_name = _group_name
AND status = 'uninitialized'
LIMIT 1;
$$;

CREATE OR REPLACE FUNCTION __schema__.set_subscription_to_active(
  _group_name text,
  _name text
)
  RETURNS void
  LANGUAGE sql
AS
$$
DELETE FROM __schema__.checkpoints
WHERE group_name = _group_name
AND name = _name
AND stream_name = '$initializing';

UPDATE __schema__.subscriptions
SET status = 'active'
WHERE group_name = _group_name
AND name = _name;
$$;

CREATE OR REPLACE FUNCTION __schema__.set_subscription_status(
  _group_name text,
  _name text,
  _status __schema__.subscription_status
)
  RETURNS void
  LANGUAGE sql
AS
$$
UPDATE __schema__.subscriptions
SET status = _status
WHERE group_name = _group_name
AND name = _name;
$$;

-------------------------------------------------
-- CHECKPOINTS
-------------------------------------------------

CREATE OR REPLACE FUNCTION __schema__.ensure_checkpoint_exists(
  _group_name text,
  _name text,
  _stream_name text
)
  RETURNS bigint
  LANGUAGE sql
AS
$$
WITH new_checkpoint AS (
  INSERT INTO __schema__.checkpoints (group_name, name, stream_name)
  VALUES (_group_name, _name, _stream_name)
  ON CONFLICT (group_name, name, stream_name) DO NOTHING
  RETURNING 0 as stream_version
)
SELECT stream_version
FROM __schema__.checkpoints
WHERE group_name = _group_name
AND name = _name
AND stream_name = _stream_name
UNION ALL
SELECT stream_version
FROM new_checkpoint;
$$;

CREATE OR REPLACE FUNCTION __schema__.lock_checkpoint(
  _group_name text,
  _name text,
  _stream_name text
)
  RETURNS TABLE (
    id bigint,
    stream_position bigint
  )
  LANGUAGE sql
AS
$$
SELECT id, stream_position
FROM __schema__.checkpoints
WHERE group_name = _group_name
AND name = _name
AND stream_name = _stream_name
FOR UPDATE
SKIP LOCKED;
$$;

CREATE OR REPLACE FUNCTION __schema__.record_checkpoint_error(
  _id bigint,
  _stream_position bigint,
  _status __schema__.checkpoint_status,
  _attempt int,
  _error jsonb,
  _process_at timestamp with time zone
)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
BEGIN
  UPDATE __schema__.checkpoints
  SET stream_position = _stream_position,
      process_at = _process_at,
      reserved_until = NULL,
      status = _status,
      retries = array_append(
        coalesce(retries, array[]::__schema__.retry[]),
        row(_attempt, _error, now())::__schema__.retry
      )
  WHERE id = _id;
END;
$$;

CREATE OR REPLACE FUNCTION __schema__.record_checkpoints(
  _checkpoints __schema__.checkpoint[]
)
  RETURNS void
  LANGUAGE sql
AS
$$
INSERT INTO __schema__.checkpoints (stream_version, stream_position, group_name, name, stream_name)
SELECT c.stream_version, c.stream_position, c.group_name, c.name, c.stream_name
FROM unnest(_checkpoints) c
ON CONFLICT (group_name, name, stream_name) DO UPDATE
  SET stream_version = excluded.stream_version;
$$;

CREATE OR REPLACE FUNCTION __schema__.recover_expired_checkpoint_reservations(
  _group_name text,
  _batch_size int
)
  RETURNS void
  LANGUAGE sql
AS
$$
UPDATE __schema__.checkpoints c
SET reserved_until = NULL
FROM (
    SELECT id
    FROM __schema__.checkpoints
    WHERE group_name = _group_name
    AND reserved_until <= now()
    FOR UPDATE SKIP LOCKED
    LIMIT _batch_size
) as d
WHERE c.id = d.id;
$$;

CREATE OR REPLACE FUNCTION __schema__.release_checkpoint_reservation(
  _id bigint
)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
BEGIN
  UPDATE __schema__.checkpoints
  SET process_at = NULL,
      reserved_until = NULL
  WHERE id = _id;
END;
$$;

CREATE OR REPLACE FUNCTION __schema__.reserve_next_available_checkpoint(
  _group_name text,
  _reservation_timeout interval
)
  RETURNS TABLE (
    id bigint,
    group_name text,
    name text,
    stream_name text,
    stream_position bigint,
    stream_version bigint,
    retry_attempts int,
    status __schema__.checkpoint_status
  )
  LANGUAGE sql
AS
$$
UPDATE __schema__.checkpoints c
SET reserved_until = now() + _reservation_timeout
FROM (
  SELECT c.id
  FROM __schema__.checkpoints c
  INNER JOIN __schema__.subscriptions s ON c.group_name = s.group_name AND c.name = s.name
  WHERE c.group_name = _group_name
  AND c.process_at <= now()
  AND c.reserved_until IS NULL
  AND s.status = 'active'
  ORDER BY c.process_at
  LIMIT 1
  FOR UPDATE
  SKIP LOCKED
) as d
WHERE c.id = d.id
RETURNING
  c.id,
  c.group_name,
  c.name,
  c.stream_name,
  c.stream_position,
  c.stream_version,
  coalesce(array_length(c.retries, 1), 0) as retry_attempts,
  c.status;
$$;

CREATE OR REPLACE FUNCTION __schema__.update_system_checkpoint_position(
  _id bigint,
  _position bigint
)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
BEGIN
  UPDATE __schema__.checkpoints
  SET stream_version = _position,
      stream_position = _position
  WHERE id = _id;
END;
$$;

CREATE OR REPLACE FUNCTION __schema__.update_checkpoint_position(
  _id bigint,
  _stream_position bigint,
  _process_at timestamp with time zone
)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
BEGIN
  UPDATE __schema__.checkpoints
  SET stream_position = _stream_position,
      process_at = _process_at,
      reserved_until = NULL,
      status = 'active',
      retries = NULL
  WHERE id = _id;
END;
$$;

-------------------------------------------------
-- METRICS
-------------------------------------------------
CREATE OR REPLACE FUNCTION __schema__.get_subscription_lag_count()
  RETURNS bigint
  LANGUAGE sql
AS
$$
WITH metric AS (
    SELECT
    FROM __schema__.subscriptions s
    INNER JOIN __schema__.checkpoints c ON s.group_name = c.group_name AND s.name = c.name
    WHERE s.status = 'active'
    AND c.status = 'active'
    AND c.lagging = true
    GROUP BY c.group_name, c.name
)
SELECT count(*)
FROM metric;
$$;

CREATE OR REPLACE FUNCTION __schema__.get_subscription_retry_count()
  RETURNS bigint
  LANGUAGE sql
AS
$$
WITH metric AS (
    SELECT count(*) as value
    FROM __schema__.subscriptions s
    INNER JOIN __schema__.checkpoints c ON s.group_name = c.group_name AND s.name = c.name
    WHERE s.status != 'uninitialized'
    AND c.status = 'retry'
    UNION ALL
    SELECT 0
)
SELECT value
FROM metric
LIMIT 1;
$$;

CREATE OR REPLACE FUNCTION __schema__.get_subscription_failed_count()
  RETURNS bigint
  LANGUAGE sql
AS
$$
WITH metric AS (
    SELECT count(*) as value
    FROM __schema__.subscriptions s
    INNER JOIN __schema__.checkpoints c ON s.group_name = c.group_name AND s.name = c.name
    WHERE s.status != 'uninitialized'
    AND c.status = 'failed'
    UNION ALL
    SELECT 0
)
SELECT value
FROM metric
LIMIT 1;
$$;

-------------------------------------------------
-- DASHBOARD SUPPORT
-------------------------------------------------
CREATE TABLE IF NOT EXISTS __schema__.categories
(
  name text NOT NULL PRIMARY KEY,
  updated_at timestamp with time zone DEFAULT now() NOT NULL
);

GRANT UPDATE, DELETE ON __schema__.categories TO beckett;

CREATE TABLE IF NOT EXISTS __schema__.tenants
(
  tenant text NOT NULL PRIMARY KEY
);

GRANT UPDATE, DELETE ON __schema__.tenants TO beckett;

-- insert default record
INSERT INTO __schema__.tenants (tenant) VALUES ('default');

CREATE OR REPLACE FUNCTION __schema__.record_stream_data(
  _category_names text[],
  _category_timestamps timestamp with time zone[],
  _tenants text[]
)
  RETURNS void
  LANGUAGE sql
AS
$$
INSERT INTO __schema__.categories (name, updated_at)
SELECT d.name, d.timestamp
FROM unnest(_category_names, _category_timestamps) AS d (name, timestamp)
ON CONFLICT (name) DO UPDATE
  SET updated_at = excluded.updated_at;

INSERT INTO __schema__.tenants (tenant)
SELECT d.tenant
FROM unnest(_tenants) AS d (tenant)
ON CONFLICT (tenant) DO NOTHING;
$$;

-------------------------------------------------
-- UTILITIES
-------------------------------------------------
CREATE OR REPLACE FUNCTION __schema__.try_advisory_lock(
  _key text
)
  RETURNS boolean
  LANGUAGE sql
AS
$$
SELECT pg_try_advisory_lock(abs(hashtextextended(_key, 0)));
$$;

CREATE OR REPLACE FUNCTION __schema__.advisory_unlock(
  _key text
)
  RETURNS boolean
  LANGUAGE sql
AS
$$
SELECT pg_advisory_unlock(abs(hashtextextended(_key, 0)));
$$;

CREATE OR REPLACE FUNCTION __schema__.delete_subscription(_group_name text, _name text)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
BEGIN
  DELETE FROM __schema__.checkpoints
  WHERE group_name = _group_name
  AND name = _name;

  DELETE FROM __schema__.subscriptions
  WHERE group_name = _group_name
  AND name = _name;
END;
$$;
