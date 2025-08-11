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

CREATE TYPE __schema__.stream_index_type AS
(
  stream_name text,
  category text,
  latest_position bigint,
  latest_global_position bigint,
  message_count bigint
);

CREATE TYPE __schema__.message_index_type AS
(
  id uuid,
  global_position bigint,
  stream_name text,
  stream_position bigint,
  message_type_name text,
  category text,
  correlation_id text,
  tenant text,
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
  category text NULL,
  stream_name text NULL,
  message_types text[] NULL,
  priority integer NOT NULL DEFAULT 2147483647,
  skip_during_replay boolean NOT NULL DEFAULT false,
  UNIQUE (subscription_group_id, name)
);

CREATE INDEX ix_subscriptions_reservation_candidates ON __schema__.subscriptions (subscription_group_id, name, status) WHERE status = 'active' OR status = 'replay';
CREATE INDEX IF NOT EXISTS ix_subscriptions_category ON __schema__.subscriptions (category) WHERE category IS NOT NULL;

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

CREATE INDEX IF NOT EXISTS ix_checkpoints_metrics ON __schema__.checkpoints (status, subscription_id);
CREATE INDEX IF NOT EXISTS ix_checkpoints_subscription_id ON __schema__.checkpoints (subscription_id);

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
CREATE TABLE IF NOT EXISTS __schema__.stream_categories
(
  id bigint PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
  name text NOT NULL UNIQUE,
  updated_at timestamp with time zone DEFAULT now() NOT NULL
);

GRANT UPDATE, DELETE ON __schema__.stream_categories TO beckett;

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
  _rows integer;
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

      GET DIAGNOSTICS _rows = ROW_COUNT;
      EXIT WHEN _rows = 0;
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
  _rows integer;
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
    LEFT JOIN __schema__.checkpoints_ready cr ON c.id = cr.id
    LEFT JOIN __schema__.checkpoints_reserved cres ON c.id = cres.id
    INNER JOIN __schema__.messages_active m ON c.stream_name = m.stream_name
        AND COALESCE(cr.target_stream_version, cres.target_stream_version, c.stream_position) = m.stream_position
    WHERE c.subscription_id = _subscription_id;

    -- Store original stream positions for replay ready queue, then reset positions
    CREATE TEMP TABLE IF NOT EXISTS replay_checkpoints AS
    SELECT c.id, c.stream_position as target_stream_version, sg.name as subscription_group_name
    FROM __schema__.checkpoints c
    INNER JOIN __schema__.subscriptions s ON c.subscription_id = s.id
    INNER JOIN __schema__.subscription_groups sg ON s.subscription_group_id = sg.id
    WHERE c.subscription_id = _subscription_id
    AND c.stream_name != '$initializing';

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

      GET DIAGNOSTICS _rows = ROW_COUNT;
      EXIT WHEN _rows = 0;
    END LOOP;

    UPDATE __schema__.subscriptions
    SET status = 'replay',
        replay_target_position = _replay_target_position
    WHERE id = _subscription_id;

    -- Add checkpoints to ready queue for processing in batches using original positions
    LOOP
      INSERT INTO __schema__.checkpoints_ready (id, process_at, subscription_group_name, target_stream_version)
      SELECT rc.id, now(), rc.subscription_group_name, rc.target_stream_version
      FROM replay_checkpoints rc
      WHERE rc.id IN (
        SELECT id
        FROM replay_checkpoints
        WHERE id NOT IN (SELECT id FROM __schema__.checkpoints_ready WHERE id IN (SELECT id FROM replay_checkpoints))
        LIMIT 500
      )
      ON CONFLICT (id) DO UPDATE
          SET process_at = EXCLUDED.process_at,
              target_stream_version = EXCLUDED.target_stream_version;

      GET DIAGNOSTICS _rows = ROW_COUNT;
      EXIT WHEN _rows = 0;
    END LOOP;

    DROP TABLE IF EXISTS replay_checkpoints;

    PERFORM pg_notify('beckett:checkpoints', _subscription_id::text);
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
  _rows integer;
BEGIN
  SELECT s.id INTO _subscription_id
  FROM __schema__.subscriptions s
  INNER JOIN __schema__.subscription_groups sg ON s.subscription_group_id = sg.id
  WHERE sg.name = _group_name
  AND s.name = _name;

  IF _subscription_id IS NOT NULL THEN
    -- Store original stream positions for reset ready queue, then delete checkpoints
    CREATE TEMP TABLE IF NOT EXISTS reset_checkpoints AS
    SELECT c.stream_name, c.stream_position as target_stream_version, sg.name as subscription_group_name
    FROM __schema__.checkpoints c
    INNER JOIN __schema__.subscriptions s ON c.subscription_id = s.id
    INNER JOIN __schema__.subscription_groups sg ON s.subscription_group_id = sg.id
    WHERE c.subscription_id = _subscription_id
    AND c.stream_name != '$initializing';

    LOOP
      DELETE FROM __schema__.checkpoints
      WHERE subscription_id = _subscription_id
      AND id IN (
        SELECT id FROM __schema__.checkpoints
        WHERE subscription_id = _subscription_id
        LIMIT 500
      );

      GET DIAGNOSTICS _rows = ROW_COUNT;
      EXIT WHEN _rows = 0;
    END LOOP;

    UPDATE __schema__.subscriptions
    SET status = 'uninitialized',
        replay_target_position = NULL
    WHERE id = _subscription_id;

    INSERT INTO __schema__.checkpoints (subscription_id, stream_name)
    VALUES (_subscription_id, '$initializing')
    ON CONFLICT (subscription_id, stream_name) DO UPDATE
      SET stream_position = 0;

    -- Recreate checkpoints from stored data and add to ready queue
    LOOP
      WITH new_checkpoints AS (
        INSERT INTO __schema__.checkpoints (subscription_id, stream_name)
        SELECT _subscription_id, rc.stream_name
        FROM reset_checkpoints rc
        WHERE rc.stream_name NOT IN (
          SELECT stream_name FROM __schema__.checkpoints WHERE subscription_id = _subscription_id
        )
        LIMIT 500
        RETURNING id, stream_name
      )
      INSERT INTO __schema__.checkpoints_ready (id, process_at, subscription_group_name, target_stream_version)
      SELECT nc.id, now(), rc.subscription_group_name, rc.target_stream_version
      FROM new_checkpoints nc
      INNER JOIN reset_checkpoints rc ON nc.stream_name = rc.stream_name
      ON CONFLICT (id) DO UPDATE
          SET process_at = EXCLUDED.process_at,
              target_stream_version = EXCLUDED.target_stream_version;

      GET DIAGNOSTICS _rows = ROW_COUNT;
      EXIT WHEN _rows = 0;
    END LOOP;

    DROP TABLE IF EXISTS reset_checkpoints;

    PERFORM pg_notify('beckett:subscriptions:reset', _group_name);
  END IF;
END;
$$;

-------------------------------------------------
-- GLOBAL READER ARCHITECTURE
-------------------------------------------------

-- Global reader checkpoint table
CREATE TABLE IF NOT EXISTS __schema__.global_reader_checkpoint (
    id bigint PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    position bigint NOT NULL DEFAULT 0,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);

GRANT UPDATE ON __schema__.global_reader_checkpoint TO beckett;

-- Insert initial record
INSERT INTO __schema__.global_reader_checkpoint (position) VALUES (0);

-- Stream index table
CREATE TABLE IF NOT EXISTS __schema__.stream_index (
    id bigint PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    stream_category_id bigint NOT NULL REFERENCES __schema__.stream_categories(id),
    stream_name text NOT NULL UNIQUE,
    latest_position bigint NOT NULL,
    latest_global_position bigint NOT NULL,
    message_count bigint NOT NULL DEFAULT 1,
    first_seen_at timestamp with time zone DEFAULT now() NOT NULL,
    last_updated_at timestamp with time zone DEFAULT now() NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_stream_index_category ON __schema__.stream_index (stream_category_id);
CREATE INDEX IF NOT EXISTS ix_stream_index_last_updated ON __schema__.stream_index (last_updated_at DESC);

GRANT UPDATE, DELETE ON __schema__.stream_index TO beckett;

-- Normalized message_types table
CREATE TABLE IF NOT EXISTS __schema__.message_types (
    id bigint PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    name text NOT NULL UNIQUE,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);

GRANT UPDATE, DELETE ON __schema__.message_types TO beckett;

-- Message index table (without data)
CREATE TABLE IF NOT EXISTS __schema__.message_index (
    id uuid NOT NULL,
    stream_index_id bigint NOT NULL REFERENCES __schema__.stream_index(id),
    global_position bigint NOT NULL,
    stream_position bigint NOT NULL,
    message_type_id bigint NOT NULL REFERENCES __schema__.message_types(id),
    correlation_id text NULL,
    tenant text NULL,
    timestamp timestamp with time zone NOT NULL,
    PRIMARY KEY (global_position, id)
) PARTITION BY RANGE (global_position);

-- Create initial partition for active messages
CREATE TABLE IF NOT EXISTS __schema__.message_index_active PARTITION OF __schema__.message_index
    FOR VALUES FROM (0) TO (MAXVALUE);

-- Create indexes on the partition
CREATE INDEX IF NOT EXISTS ix_message_index_active_stream_type ON __schema__.message_index_active (stream_index_id, message_type_id);
CREATE INDEX IF NOT EXISTS ix_message_index_stream_index_id ON __schema__.message_index (stream_index_id);
CREATE INDEX IF NOT EXISTS ix_message_index_message_type_id ON __schema__.message_index (message_type_id);
CREATE INDEX IF NOT EXISTS ix_message_index_active_correlation_id ON __schema__.message_index_active (correlation_id)
    WHERE correlation_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_message_index_active_tenant ON __schema__.message_index_active (tenant)
    WHERE tenant IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_message_index_active_timestamp ON __schema__.message_index_active (timestamp DESC);

GRANT UPDATE, DELETE ON __schema__.message_index TO beckett;
GRANT UPDATE, DELETE ON __schema__.message_index_active TO beckett;

-- Stream message types lookup table for fast initialization
CREATE TABLE IF NOT EXISTS __schema__.stream_message_types (
    stream_index_id bigint NOT NULL REFERENCES __schema__.stream_index(id),
    message_type_id bigint NOT NULL REFERENCES __schema__.message_types(id),
    first_seen_at timestamp with time zone DEFAULT now() NOT NULL,
    last_seen_at timestamp with time zone DEFAULT now() NOT NULL,
    message_count bigint NOT NULL DEFAULT 1,
    PRIMARY KEY (stream_index_id, message_type_id)
);

CREATE INDEX IF NOT EXISTS ix_stream_message_types_message_type_id ON __schema__.stream_message_types (message_type_id);
CREATE INDEX IF NOT EXISTS ix_stream_message_types_stream_index_id ON __schema__.stream_message_types (stream_index_id);

GRANT UPDATE, DELETE ON __schema__.stream_message_types TO beckett;

-- Subscription message types junction table
CREATE TABLE IF NOT EXISTS __schema__.subscription_message_types (
    subscription_id bigint NOT NULL REFERENCES __schema__.subscriptions(id) ON DELETE CASCADE,
    message_type_id bigint NOT NULL REFERENCES __schema__.message_types(id) ON DELETE CASCADE,
    PRIMARY KEY (subscription_id, message_type_id)
);

GRANT UPDATE, DELETE ON __schema__.subscription_message_types TO beckett;
