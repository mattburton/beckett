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

CREATE INDEX IF NOT EXISTS ix_messages_active_correlation_id_global_position ON __schema__.messages_active ((metadata ->> '$correlation_id'), global_position)
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

CREATE OR REPLACE FUNCTION __schema__.assert_condition(
  _condition boolean,
  _message text
)
  RETURNS boolean
  IMMUTABLE
  LANGUAGE plpgsql
AS
$$
BEGIN
  IF NOT _condition THEN
    RAISE EXCEPTION '%', _message;
  END IF;
  RETURN TRUE;
END;
$$;

-------------------------------------------------
-- SCHEDULED MESSAGE SUPPORT
-------------------------------------------------

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
  'unknown',
  'replay',
  'backfill'
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
  replay_target_position bigint NULL,
  PRIMARY KEY (group_name, name)
);

CREATE INDEX ix_subscriptions_reservation_candidates ON __schema__.subscriptions (group_name, name, status) WHERE status = 'active' OR status = 'replay';

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
  retry_attempts int NOT NULL DEFAULT 0,
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

  IF (NEW.process_at IS NOT NULL AND NEW.reserved_until IS NULL) THEN
    PERFORM pg_notify('beckett:checkpoints', NEW.group_name);
  END IF;

  RETURN NEW;
END;
$$;

CREATE TRIGGER checkpoint_preprocessor BEFORE INSERT OR UPDATE ON __schema__.checkpoints
  FOR EACH ROW EXECUTE FUNCTION __schema__.checkpoint_preprocessor();

GRANT UPDATE, DELETE ON __schema__.checkpoints TO beckett;

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

-------------------------------------------------
-- SUBSCRIPTION UTILITY FUNCTIONS
-------------------------------------------------
CREATE OR REPLACE FUNCTION __schema__.delete_subscription(
  _group_name text,
  _name text
)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
DECLARE
  _rows_deleted integer;
BEGIN
  LOOP
    DELETE FROM __schema__.checkpoints
    WHERE id IN (
      SELECT id
      FROM __schema__.checkpoints
      WHERE group_name = _group_name
      AND name = _name
      LIMIT 500
    );

    GET DIAGNOSTICS _rows_deleted = ROW_COUNT;
    EXIT WHEN _rows_deleted = 0;
  END LOOP;

  DELETE FROM __schema__.subscriptions
  WHERE group_name = _group_name
  AND name = _name;
END;
$$;

CREATE OR REPLACE FUNCTION __schema__.move_subscription(
  _group_name text,
  _name text,
  _new_group_name text
)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
DECLARE
  _rows_updated integer;
BEGIN
  UPDATE __schema__.subscriptions
  SET group_name = _new_group_name
  WHERE group_name = _group_name
  AND name = _name;

  LOOP
    UPDATE __schema__.checkpoints
    SET group_name = _new_group_name
    WHERE id IN (
      SELECT id
      FROM __schema__.checkpoints
      WHERE group_name = _group_name
      AND name = _name
      LIMIT 500
    );

    GET DIAGNOSTICS _rows_updated = ROW_COUNT;
    EXIT WHEN _rows_updated = 0;
  END LOOP;
END;
$$;

CREATE OR REPLACE FUNCTION __schema__.rename_subscription(
  _group_name text,
  _name text,
  _new_name text
)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
DECLARE
  _rows_updated integer;
BEGIN
  UPDATE __schema__.subscriptions
  SET name = _new_name
  WHERE group_name = _group_name
  AND name = _name;

  LOOP
    UPDATE __schema__.checkpoints
    SET name = _new_name
    WHERE id IN (
      SELECT id
      FROM __schema__.checkpoints
      WHERE group_name = _group_name
      AND name = _name
      LIMIT 500
    );

    GET DIAGNOSTICS _rows_updated = ROW_COUNT;
    EXIT WHEN _rows_updated = 0;
  END LOOP;
END;
$$;

CREATE OR REPLACE FUNCTION __schema__.replay_subscription(
  _group_name text,
  _name text
)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
DECLARE
  _replay_target_position bigint;
  _rows_updated integer;
BEGIN
  SELECT coalesce(max(m.global_position), 0)
  INTO _replay_target_position
  FROM beckett.checkpoints c
  INNER JOIN beckett.messages_active m ON c.stream_name = m.stream_name AND c.stream_version = m.stream_position
  WHERE c.group_name = _group_name
  AND c.name = _name;

  LOOP
    UPDATE __schema__.checkpoints
    SET stream_position = 0
    WHERE id IN (
      SELECT id
      FROM __schema__.checkpoints
      WHERE group_name = _group_name
      AND name = _name
      AND stream_position > 0
      LIMIT 500
    );

    GET DIAGNOSTICS _rows_updated = ROW_COUNT;
    EXIT WHEN _rows_updated = 0;
  END LOOP;

  UPDATE __schema__.subscriptions
  SET status = 'replay',
      replay_target_position = _replay_target_position
  WHERE group_name = _group_name
  AND name = _name;
END;
$$;

CREATE OR REPLACE FUNCTION __schema__.reset_subscription(
  _group_name text,
  _name text
)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
DECLARE
  _rows_deleted integer;
BEGIN
  LOOP
    DELETE FROM __schema__.checkpoints
    WHERE group_name = _group_name AND name = _name
    AND id IN (
      SELECT id FROM __schema__.checkpoints
      WHERE group_name = _group_name AND name = _name
      LIMIT 500
    );

    GET DIAGNOSTICS _rows_deleted = ROW_COUNT;
    EXIT WHEN _rows_deleted = 0;
  END LOOP;

  UPDATE __schema__.subscriptions
  SET status = 'uninitialized',
      replay_target_position = NULL
  WHERE group_name = _group_name
  AND name = _name;

  INSERT INTO __schema__.checkpoints (group_name, name, stream_name)
  VALUES (_group_name, _name, '$initializing')
  ON CONFLICT (group_name, name, stream_name) DO UPDATE
    SET stream_version = 0,
        stream_position = 0;
END;
$$;
