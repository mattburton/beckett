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
-- RECURRING MESSAGE SUPPORT
-------------------------------------------------

CREATE TABLE __schema__.recurring_messages
(
  name text NOT NULL,
  cron_expression text NOT NULL,
  time_zone_id text NOT NULL,
  stream_name text NOT NULL,
  type text NOT NULL,
  data jsonb NOT NULL,
  metadata jsonb NOT NULL,
  next_occurrence timestamp with time zone NULL,
  timestamp timestamp with time zone DEFAULT now() NOT NULL,
  PRIMARY KEY (name)
);

CREATE INDEX ix_recurring_messages_next_occurrence ON __schema__.recurring_messages (next_occurrence ASC)
  WHERE next_occurrence IS NOT NULL;

GRANT UPDATE, DELETE ON __schema__.recurring_messages TO beckett;

-------------------------------------------------
-- SUBSCRIPTION SUPPORT
-------------------------------------------------

CREATE TYPE __schema__.checkpoint AS
(
  subscription_id bigint,
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

CREATE TABLE IF NOT EXISTS __schema__.subscription_groups
(
  id bigint PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
  name text NOT NULL UNIQUE
);

GRANT UPDATE, DELETE ON __schema__.subscription_groups TO beckett;

CREATE TABLE IF NOT EXISTS __schema__.subscriptions
(
  id bigint PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
  subscription_group_id bigint NOT NULL REFERENCES __schema__.subscription_groups(id) ON DELETE CASCADE,
  name text NOT NULL,
  status __schema__.subscription_status DEFAULT 'uninitialized' NOT NULL,
  replay_target_position bigint NULL,
  UNIQUE (subscription_group_id, name)
);

CREATE INDEX ix_subscriptions_reservation_candidates ON __schema__.subscriptions (subscription_group_id, name, status) WHERE status = 'active' OR status = 'replay';

GRANT UPDATE, DELETE ON __schema__.subscriptions TO beckett;

CREATE TABLE IF NOT EXISTS __schema__.checkpoints
(
  id bigint PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
  subscription_id bigint NOT NULL REFERENCES __schema__.subscriptions(id) ON DELETE CASCADE,
  stream_position bigint NOT NULL DEFAULT 0,
  created_at timestamp with time zone DEFAULT now(),
  updated_at timestamp with time zone DEFAULT now(),
  retry_attempts int NOT NULL DEFAULT 0,
  status __schema__.checkpoint_status NOT NULL DEFAULT 'active',
  stream_name text NOT NULL,
  retries __schema__.retry[] NULL,
  UNIQUE (subscription_id, stream_name)
);

CREATE INDEX IF NOT EXISTS ix_checkpoints_subscription_id ON beckett.checkpoints (subscription_id);

-- ix_checkpoints_to_process and ix_checkpoints_reserved removed in v0.23.3
-- Scheduling and reservations now handled by dedicated tables

CREATE INDEX IF NOT EXISTS ix_checkpoints_metrics ON __schema__.checkpoints (status, subscription_id);

CREATE INDEX IF NOT EXISTS ix_checkpoints_subscription_id ON __schema__.checkpoints (subscription_id);

-- Checkpoint trigger functions and triggers removed in v0.23.1
-- All checkpoint state management is now handled manually in application queries

GRANT UPDATE, DELETE ON __schema__.checkpoints TO beckett;

CREATE TABLE IF NOT EXISTS __schema__.checkpoints_ready
(
    id bigint NOT NULL REFERENCES __schema__.checkpoints(id) ON DELETE CASCADE,
    target_stream_version bigint NOT NULL,
    process_at timestamp with time zone NOT NULL DEFAULT now(),
    subscription_group_name text NOT NULL,
    PRIMARY KEY (id)
);

CREATE INDEX ix_checkpoints_ready_group_process_at ON __schema__.checkpoints_ready (subscription_group_name, process_at, id);

GRANT SELECT, INSERT, UPDATE, DELETE ON __schema__.checkpoints_ready TO beckett;

CREATE TABLE IF NOT EXISTS __schema__.checkpoints_reserved
(
    id bigint NOT NULL REFERENCES __schema__.checkpoints(id) ON DELETE CASCADE,
    target_stream_version bigint NOT NULL,
    reserved_until timestamp with time zone NOT NULL,
    PRIMARY KEY (id)
);

CREATE INDEX ix_checkpoints_reserved_reserved_until ON __schema__.checkpoints_reserved (reserved_until);

GRANT SELECT, INSERT, UPDATE, DELETE ON __schema__.checkpoints_reserved TO beckett;

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
  _subscription_id bigint;
  _rows_deleted integer;
BEGIN
  SELECT s.id INTO _subscription_id
  FROM __schema__.subscriptions s
  INNER JOIN __schema__.subscription_groups sg ON s.subscription_group_id = sg.id
  WHERE sg.name = _group_name
  AND s.name = _name;

  IF _subscription_id IS NOT NULL THEN
    LOOP
      DELETE FROM __schema__.checkpoints
      WHERE id IN (
        SELECT id
        FROM __schema__.checkpoints
        WHERE subscription_id = _subscription_id
        LIMIT 500
      );

      GET DIAGNOSTICS _rows_deleted = ROW_COUNT;
      EXIT WHEN _rows_deleted = 0;
    END LOOP;

    DELETE FROM __schema__.subscriptions WHERE id = _subscription_id;
  END IF;
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
  _subscription_id bigint;
  _new_subscription_group_id bigint;
BEGIN
  SELECT s.id INTO _subscription_id
  FROM __schema__.subscriptions s
  INNER JOIN __schema__.subscription_groups sg ON s.subscription_group_id = sg.id
  WHERE sg.name = _group_name
  AND s.name = _name;

  SELECT id INTO _new_subscription_group_id
  FROM __schema__.subscription_groups
  WHERE name = _new_group_name;

  IF _subscription_id IS NOT NULL AND _new_subscription_group_id IS NOT NULL THEN
    UPDATE __schema__.subscriptions
    SET subscription_group_id = _new_subscription_group_id
    WHERE id = _subscription_id;
  END IF;
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
  _subscription_id bigint;
BEGIN
  SELECT s.id INTO _subscription_id
  FROM __schema__.subscriptions s
  INNER JOIN __schema__.subscription_groups sg ON s.subscription_group_id = sg.id
  WHERE sg.name = _group_name
  AND s.name = _name;

  IF _subscription_id IS NOT NULL THEN
    UPDATE __schema__.subscriptions
    SET name = _new_name
    WHERE id = _subscription_id;
  END IF;
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
  _subscription_id bigint;
  _replay_target_position bigint;
  _rows_updated integer;
BEGIN
  SELECT s.id INTO _subscription_id
  FROM __schema__.subscriptions s
  INNER JOIN __schema__.subscription_groups sg ON s.subscription_group_id = sg.id
  WHERE sg.name = _group_name
  AND s.name = _name;

  IF _subscription_id IS NOT NULL THEN
    SELECT coalesce(max(m.global_position), 0)
    INTO _replay_target_position
    FROM __schema__.checkpoints c
    INNER JOIN __schema__.messages_active m ON c.stream_name = m.stream_name AND c.stream_version = m.stream_position
    WHERE c.subscription_id = _subscription_id;

    LOOP
      UPDATE __schema__.checkpoints
      SET stream_position = 0
      WHERE id IN (
        SELECT id
        FROM __schema__.checkpoints
        WHERE subscription_id = _subscription_id
        AND stream_position > 0
        LIMIT 500
      );

      GET DIAGNOSTICS _rows_updated = ROW_COUNT;
      EXIT WHEN _rows_updated = 0;
    END LOOP;

    UPDATE __schema__.subscriptions
    SET status = 'replay',
        replay_target_position = _replay_target_position
    WHERE id = _subscription_id;
  END IF;
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
  _subscription_id bigint;
  _rows_deleted integer;
BEGIN
  SELECT s.id INTO _subscription_id
  FROM __schema__.subscriptions s
  INNER JOIN __schema__.subscription_groups sg ON s.subscription_group_id = sg.id
  WHERE sg.name = _group_name
  AND s.name = _name;

  IF _subscription_id IS NOT NULL THEN
    LOOP
      DELETE FROM __schema__.checkpoints
      WHERE subscription_id = _subscription_id
      AND id IN (
        SELECT id FROM __schema__.checkpoints
        WHERE subscription_id = _subscription_id
        LIMIT 500
      );

      GET DIAGNOSTICS _rows_deleted = ROW_COUNT;
      EXIT WHEN _rows_deleted = 0;
    END LOOP;

    UPDATE __schema__.subscriptions
    SET status = 'uninitialized',
        replay_target_position = NULL
    WHERE id = _subscription_id;

    INSERT INTO __schema__.checkpoints (subscription_id, stream_name)
    VALUES (_subscription_id, '$initializing')
    ON CONFLICT (subscription_id, stream_name) DO UPDATE
      SET stream_version = 0,
          stream_position = 0;
  END IF;
END;
$$;
