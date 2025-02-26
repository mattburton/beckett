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

CREATE INDEX IF NOT EXISTS ix_messages_active_tenant_stream_category on __schema__.messages_active ((metadata ->> '$tenant'), __schema__.stream_category(stream_name))
  WHERE metadata ->> '$tenant' IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_messages_active_correlation_id ON __schema__.messages_active ((metadata ->> '$correlation_id'))
  WHERE metadata ->> '$correlation_id' IS NOT NULL;

CREATE FUNCTION __schema__.stream_operations()
  RETURNS trigger
  LANGUAGE plpgsql
AS
$$
BEGIN
  IF NEW.type = '$stream_truncated' THEN
    UPDATE __schema__.messages
    SET archived = TRUE
    WHERE stream_name = NEW.stream_name
    AND stream_position < NEW.stream_position
    AND archived = FALSE;
  END IF;

  IF NEW.type = '$stream_archived' THEN
    UPDATE __schema__.messages
    SET archived = TRUE
    WHERE stream_name = NEW.stream_name
    AND stream_position < NEW.stream_position
    AND archived = FALSE;
  END IF;

  RETURN NEW;
END;
$$;

CREATE TRIGGER stream_operations
  BEFORE INSERT
  ON __schema__.messages
  FOR EACH ROW
EXECUTE FUNCTION __schema__.stream_operations();

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
  IF (_expected_version < 0) THEN
    PERFORM pg_advisory_xact_lock(__schema__.stream_hash(_stream_name));
  END IF;

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
  _read_forwards boolean DEFAULT true
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
           _stream_version AS stream_version,
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
    AND (_starting_global_position IS NULL OR m.global_position >= _starting_global_position)
    AND (_ending_global_position IS NULL OR m.global_position <= _ending_global_position)
    AND m.archived = false
    ORDER BY CASE WHEN _read_forwards = true THEN m.stream_position END,
             CASE WHEN _read_forwards = false THEN m.stream_position END DESC
    LIMIT _count;
END;
$$;

CREATE OR REPLACE FUNCTION __schema__.read_global_stream(
  _starting_global_position bigint,
  _batch_size int
)
  RETURNS TABLE (
    stream_name text,
    stream_position bigint,
    global_position bigint,
    type text
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
           m.type
    FROM __schema__.messages m
    WHERE (m.transaction_id, m.global_position) > (_transaction_id, _starting_global_position)
    AND m.transaction_id < pg_snapshot_xmin(pg_current_snapshot())
    AND m.archived = false
    ORDER BY m.transaction_id, m.global_position
    LIMIT _batch_size;
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
  subscription_id int,
  stream_name text,
  stream_version bigint
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

CREATE TABLE IF NOT EXISTS __schema__.groups
(
  id int PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
  name text NOT NULL UNIQUE,
  global_position bigint NOT NULL DEFAULT 0
);

GRANT UPDATE, DELETE ON __schema__.groups TO beckett;

CREATE TABLE IF NOT EXISTS __schema__.subscriptions
(
  id int PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
  group_id int NOT NULL REFERENCES __schema__.groups(id),
  name text NOT NULL,
  status __schema__.subscription_status DEFAULT 'uninitialized' NOT NULL,
  UNIQUE (group_id, name)
);

CREATE INDEX IF NOT EXISTS ix_subscriptions_status ON __schema__.subscriptions (id, status);

CREATE INDEX IF NOT EXISTS ix_subscriptions_active ON __schema__.subscriptions (id, group_id, status) WHERE status = 'active';

GRANT UPDATE, DELETE ON __schema__.subscriptions TO beckett;

CREATE TABLE IF NOT EXISTS __schema__.categories
(
  id int PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
  name text NOT NULL,
  updated_at timestamp with time zone NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX uix_categories_name ON __schema__.categories (name) INCLUDE (id);

GRANT UPDATE, DELETE ON __schema__.streams TO beckett;

CREATE TABLE IF NOT EXISTS __schema__.streams
(
  id bigint PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
  name text NOT NULL,
  updated_at timestamp with time zone NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX uix_streams_name ON __schema__.streams (name) INCLUDE (id);

GRANT UPDATE, DELETE ON __schema__.streams TO beckett;

CREATE TABLE IF NOT EXISTS __schema__.tenants
(
  id int PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
  name text NOT NULL
);

CREATE UNIQUE INDEX uix_tenants_name ON __schema__.tenants (name) INCLUDE (id);

GRANT UPDATE, DELETE ON __schema__.tenants TO beckett;

CREATE TABLE IF NOT EXISTS __schema__.types
(
  id int PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
  name text NOT NULL
);

CREATE UNIQUE INDEX uix_types_name ON __schema__.types (name) INCLUDE (id);

GRANT UPDATE, DELETE ON __schema__.types TO beckett;

CREATE TABLE IF NOT EXISTS __schema__.message_index
(
  stream_id bigint NOT NULL,
  global_position bigint NOT NULL PRIMARY KEY,
  stream_position bigint NOT NULL,
  timestamp timestamp with time zone DEFAULT now() NOT NULL,
  type_id int NOT NULL,
  tenant_id int NOT NULL,
  message_id text NOT NULL,
  correlation_id text NOT NULL,
  causation_id text NOT NULL,
  metadata jsonb NOT NULL,
  UNIQUE (message_id),
  UNIQUE (stream_name, stream_position),
  FOREIGN KEY (stream_id) REFERENCES __schema__.streams (id),
  FOREIGN KEY (type_id) REFERENCES __schema__.types (id),
  FOREIGN KEY (tenant_id) REFERENCES __schema__.tenants (id)
);

GRANT UPDATE, DELETE ON __schema__.types TO beckett;

CREATE TABLE IF NOT EXISTS __schema__.checkpoints
(
  id bigint PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
  stream_id bigint NOT NULL REFERENCES __schema__.streams (id),
  stream_version bigint NOT NULL DEFAULT 0,
  stream_position bigint NOT NULL DEFAULT 0,
  created_at timestamp with time zone DEFAULT now(),
  updated_at timestamp with time zone DEFAULT now(),
  process_at timestamp with time zone NULL,
  reserved_until timestamp with time zone NULL,
  subscription_id int NOT NULL REFERENCES __schema__.subscriptions(id),
  lagging boolean GENERATED ALWAYS AS (stream_version > stream_position) STORED,
  status __schema__.checkpoint_status NOT NULL DEFAULT 'active',
  retries __schema__.retry[] NULL,
  UNIQUE (subscription_id, stream_id)
);

CREATE INDEX IF NOT EXISTS ix_checkpoints_to_process ON __schema__.checkpoints (subscription_id, process_at, reserved_until)
  WHERE process_at IS NOT NULL AND reserved_until IS NULL;

CREATE INDEX IF NOT EXISTS ix_checkpoints_reserved ON __schema__.checkpoints (subscription_id, reserved_until)
  WHERE reserved_until IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_checkpoints_metrics ON __schema__.checkpoints (subscription_id, status, lagging);

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

  IF (NEW.process_at IS NOT NULL AND NEW.reserved_until IS NULL) THEN
    PERFORM pg_notify('beckett:checkpoints', NEW.subscription_id::text);
  END IF;

  RETURN NEW;
END;
$$;

CREATE TRIGGER checkpoint_preprocessor BEFORE INSERT OR UPDATE ON __schema__.checkpoints
  FOR EACH ROW EXECUTE FUNCTION __schema__.checkpoint_preprocessor();

GRANT UPDATE, DELETE ON __schema__.checkpoints TO beckett;

-------------------------------------------------
-- GROUPS
-------------------------------------------------

CREATE OR REPLACE FUNCTION __schema__.get_or_add_group(
  _name text
)
  RETURNS TABLE (
    id int
  )
  LANGUAGE sql
AS
$$
WITH existing_group_id AS (
  SELECT id
  FROM __schema__.groups
  where name = _name
),
new_group_id AS (
  INSERT INTO __schema__.groups (name)
  SELECT _name
  WHERE NOT EXISTS (SELECT id FROM existing_group_id)
  ON CONFLICT (name) DO NOTHING
  RETURNING id
)
SELECT id
FROM existing_group_id
UNION ALL
SELECT id
FROM new_group_id
LIMIT 1;
$$;

CREATE OR REPLACE FUNCTION __schema__.lock_group(
  _id int
)
  RETURNS TABLE (
    id int,
    global_position bigint
  )
  LANGUAGE sql
AS
$$
SELECT id, global_position
FROM __schema__.groups
WHERE id = _id
FOR UPDATE
SKIP LOCKED;
$$;

CREATE OR REPLACE FUNCTION __schema__.update_group_global_position(
  _id bigint,
  _global_position bigint
)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
BEGIN
  UPDATE __schema__.groups
  SET global_position = _global_position
  WHERE id = _id;
END;
$$;

-------------------------------------------------
-- SUBSCRIPTIONS
-------------------------------------------------

CREATE OR REPLACE FUNCTION __schema__.get_or_add_subscription(
  _group_id int,
  _name text
)
  RETURNS TABLE (
    id int,
    status __schema__.subscription_status
  )
  LANGUAGE sql
AS
$$
WITH existing_subscription_id AS (
  SELECT id
  FROM __schema__.subscriptions
  WHERE group_id = _group_id
  AND name = _name
)
INSERT INTO __schema__.subscriptions (group_id, name)
SELECT _group_id, _name
WHERE NOT EXISTS (SELECT id FROM existing_subscription_id)
ON CONFLICT (group_id, name) DO NOTHING;

SELECT id, status
FROM __schema__.subscriptions
WHERE group_id = _group_id
AND name = _name;
$$;

CREATE OR REPLACE FUNCTION __schema__.get_next_uninitialized_subscription(
  _group_id int
)
  RETURNS TABLE (
    id int
  )
  LANGUAGE sql
AS
$$
SELECT id
FROM __schema__.subscriptions
WHERE group_id = _group_id
AND status = 'uninitialized'
LIMIT 1;
$$;

CREATE OR REPLACE FUNCTION __schema__.pause_subscription(
  _id int
)
  RETURNS void
  LANGUAGE sql
AS
$$
UPDATE __schema__.subscriptions
SET status = 'paused'
WHERE id = _id;
$$;

CREATE OR REPLACE FUNCTION __schema__.resume_subscription(
  _id int
)
  RETURNS void
  LANGUAGE sql
AS
$$
UPDATE __schema__.subscriptions
SET status = 'active'
WHERE id = _id;

SELECT pg_notify('beckett:checkpoints', _id::text);
$$;

CREATE OR REPLACE FUNCTION __schema__.set_subscription_to_active(
  _id int
)
  RETURNS void
  LANGUAGE sql
AS
$$
DELETE FROM __schema__.checkpoints
WHERE subscription_id = _id
AND stream_id = (SELECT id FROM __schema__.streams WHERE name = '$initializing');

UPDATE __schema__.subscriptions
SET status = 'active'
WHERE id = _id;
$$;

CREATE OR REPLACE FUNCTION __schema__.set_subscription_status(
  _id int,
  _status __schema__.subscription_status
)
  RETURNS void
  LANGUAGE sql
AS
$$
UPDATE __schema__.subscriptions
SET status = _status
WHERE id = _id;
$$;

-------------------------------------------------
-- CHECKPOINTS
-------------------------------------------------

CREATE OR REPLACE FUNCTION __schema__.lock_checkpoint(
  _subscription_id int,
  _stream_name text
)
  RETURNS TABLE (
    id bigint,
    stream_position bigint
  )
  LANGUAGE sql
AS
$$
SELECT c.id, c.stream_position
FROM __schema__.checkpoints c
INNER JOIN __schema__.streams s ON c.stream_id = s.id
WHERE c.subscription_id = _subscription_id
AND s.name = _stream_name
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
WITH checkpoints_to_record AS (
  SELECT c.subscription_id, c.stream_name, c.stream_version
  FROM unnest(_checkpoints) c
),
existing_streams AS (
  SELECT s.id, s.name
  FROM __schema__.streams s
  INNER JOIN checkpoints_to_record c ON s.name = c.stream_name
),
new_streams AS (
  INSERT INTO __schema__.streams (name)
  SELECT c.stream_name
  FROM checkpoints_to_record c
  WHERE NOT EXISTS (select from existing_streams where name = c.stream_name)
  ON CONFLICT (name) DO NOTHING
  RETURNING id, name
),
streams AS (
  SELECT id, name
  FROM existing_streams
  UNION
  SELECT id, name
  FROM new_streams
)
INSERT INTO __schema__.checkpoints (stream_id, stream_version, subscription_id)
SELECT s.id, c.stream_version, c.subscription_id
FROM checkpoints_to_record c
INNER JOIN streams s on c.stream_name = s.name
ON CONFLICT (subscription_id, stream_id) DO UPDATE
  SET stream_version = excluded.stream_version;
$$;

CREATE OR REPLACE FUNCTION __schema__.recover_expired_checkpoint_reservations(
  _group_id int,
  _batch_size int
)
  RETURNS void
  LANGUAGE sql
AS
$$
UPDATE __schema__.checkpoints c
SET reserved_until = NULL
FROM (
    SELECT c.id
    FROM __schema__.checkpoints c
    INNER JOIN __schema__.subscriptions s on c.subscription_id = s.id
    WHERE s.group_id = _group_id
    AND c.reserved_until <= now()
    FOR UPDATE SKIP LOCKED
    LIMIT _batch_size
) d
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
  _group_id int,
  _reservation_timeout interval
)
  RETURNS TABLE (
    id bigint,
    subscription_id int,
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
  SELECT c.id, st.name AS stream_name
  FROM __schema__.checkpoints c
  INNER JOIN __schema__.subscriptions s ON c.subscription_id = s.id
  INNER JOIN __schema__.streams st ON c.stream_id = st.id
  WHERE s.group_id = _group_id
  AND s.status = 'active'
  AND c.process_at <= now()
  AND c.reserved_until IS NULL
  ORDER BY c.process_at
  LIMIT 1
  FOR UPDATE
  SKIP LOCKED
) d
WHERE c.id = d.id
RETURNING
  c.id,
  c.subscription_id,
  d.stream_name,
  c.stream_position,
  c.stream_version,
  coalesce(array_length(c.retries, 1), 0) AS retry_attempts,
  c.status;
$$;

CREATE OR REPLACE FUNCTION __schema__.schedule_checkpoints(
  _ids bigint[],
  _process_at timestamp with time zone
)
  RETURNS void
  LANGUAGE sql
AS
$$
UPDATE __schema__.checkpoints
SET process_at = _process_at
WHERE id = ANY(_ids);
$$;

CREATE OR REPLACE FUNCTION __schema__.skip_checkpoint_position(
  _id bigint
)
  RETURNS void
  LANGUAGE sql
AS
$$
UPDATE __schema__.checkpoints
SET stream_position = CASE WHEN stream_position + 1 > stream_version THEN stream_position ELSE stream_position + 1 END,
    process_at = NULL,
    reserved_until = NULL,
    status = 'active',
    retries = NULL
WHERE id = _id;
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
    INNER JOIN __schema__.checkpoints c ON s.id = c.subscription_id
    WHERE s.status = 'active'
    AND c.status = 'active'
    AND c.lagging = true
    GROUP BY c.subscription_id
)
SELECT count(*)
FROM metric;
$$;

CREATE OR REPLACE FUNCTION __schema__.get_subscription_retry_count()
  RETURNS bigint
  LANGUAGE sql
AS
$$
SELECT count(*)
FROM __schema__.subscriptions s
INNER JOIN __schema__.checkpoints c ON s.id = c.subscription_id
WHERE s.status != 'uninitialized'
AND c.status = 'retry';
$$;

CREATE OR REPLACE FUNCTION __schema__.get_subscription_failed_count()
  RETURNS bigint
  LANGUAGE sql
AS
$$
SELECT count(*)
FROM __schema__.subscriptions s
INNER JOIN __schema__.checkpoints c ON s.id = c.subscription_id
WHERE s.status != 'uninitialized'
AND c.status = 'failed';;
$$;

CREATE OR REPLACE FUNCTION __schema__.get_subscription_metrics()
  RETURNS TABLE (
    lagging bigint,
    retries bigint,
    failed bigint
  )
  LANGUAGE sql
AS
$$
WITH lagging AS (
    WITH lagging_subscriptions AS (
        SELECT
        FROM __schema__.subscriptions s
        INNER JOIN __schema__.checkpoints c ON s.id = c.subscription_id
        WHERE s.status = 'active'
        AND c.status = 'active'
        AND c.lagging = true
        GROUP BY c.subscription_id
    )
    SELECT count(*) AS lagging
    FROM lagging_subscriptions
),
retries AS (
    SELECT count(*) AS retries
    FROM __schema__.subscriptions s
    INNER JOIN __schema__.checkpoints c ON s.id = c.subscription_id
    WHERE s.status != 'uninitialized'
    AND c.status = 'retry'
),
failed AS (
    SELECT count(*) AS failed
    FROM __schema__.subscriptions s
    INNER JOIN __schema__.checkpoints c ON s.id = c.subscription_id
    WHERE s.status != 'uninitialized'
    AND c.status = 'failed'
)
SELECT l.lagging, r.retries, f.failed
FROM lagging AS l, retries AS r, failed AS f;
$$;

-------------------------------------------------
-- DASHBOARD SUPPORT
-------------------------------------------------
CREATE MATERIALIZED VIEW __schema__.tenants AS
SELECT metadata ->> '$tenant' AS tenant
FROM __schema__.messages_active
WHERE metadata ->> '$tenant' IS NOT NULL
GROUP BY tenant;

ALTER MATERIALIZED VIEW __schema__.tenants OWNER TO beckett;

CREATE UNIQUE INDEX on __schema__.tenants (tenant);

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
