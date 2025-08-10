-- Beckett v0.22.0 - normalize subscription data

-- Step 1: Create new subscription_groups table
CREATE TABLE IF NOT EXISTS beckett.subscription_groups
(
  id bigint PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
  name text NOT NULL UNIQUE
);

GRANT UPDATE, DELETE ON beckett.subscription_groups TO beckett;

-- Step 2: Populate subscription_groups from existing subscriptions
INSERT INTO beckett.subscription_groups (name)
SELECT DISTINCT group_name
FROM beckett.subscriptions
WHERE group_name IS NOT NULL
ON CONFLICT (name) DO NOTHING;

-- Step 3: Create new subscriptions table with optimal column ordering
CREATE TABLE beckett.subscriptions_new
(
  id bigint GENERATED ALWAYS AS IDENTITY,
  subscription_group_id bigint NOT NULL,
  name text NOT NULL,
  status beckett.subscription_status DEFAULT 'uninitialized' NOT NULL,
  replay_target_position bigint NULL
);

-- Step 4: Populate new subscriptions table
INSERT INTO beckett.subscriptions_new (subscription_group_id, name, status, replay_target_position)
SELECT sg.id, s.name, s.status, s.replay_target_position
FROM beckett.subscriptions s
INNER JOIN beckett.subscription_groups sg ON s.group_name = sg.name;

-- Step 5: Ensure $global subscriptions exist for all groups that have checkpoints with name = '$global'
INSERT INTO beckett.subscriptions_new (subscription_group_id, name, status)
SELECT DISTINCT sg.id, '$global', 'active'::beckett.subscription_status
FROM beckett.subscription_groups sg
WHERE EXISTS (
    SELECT 1
    FROM beckett.checkpoints c
    WHERE c.group_name = sg.name
    AND c.name = '$global'
)
ON CONFLICT DO NOTHING;

-- Step 6: Add constraints to new subscriptions table
ALTER TABLE beckett.subscriptions_new ADD CONSTRAINT subscriptions_new_pkey PRIMARY KEY (id);
ALTER TABLE beckett.subscriptions_new ADD CONSTRAINT subscriptions_new_subscription_group_id_name_key UNIQUE (subscription_group_id, name);
ALTER TABLE beckett.subscriptions_new ADD CONSTRAINT subscriptions_new_subscription_group_id_fkey 
    FOREIGN KEY (subscription_group_id) REFERENCES beckett.subscription_groups(id) ON DELETE CASCADE;

-- Step 7: Drop old subscriptions table and rename new one
DROP TABLE beckett.subscriptions CASCADE;
ALTER TABLE beckett.subscriptions_new RENAME TO subscriptions;

-- Rename constraints to remove "new" suffix
ALTER TABLE beckett.subscriptions RENAME CONSTRAINT subscriptions_new_pkey TO subscriptions_pkey;
ALTER TABLE beckett.subscriptions RENAME CONSTRAINT subscriptions_new_subscription_group_id_name_key TO subscriptions_subscription_group_id_name_key;
ALTER TABLE beckett.subscriptions RENAME CONSTRAINT subscriptions_new_subscription_group_id_fkey TO subscriptions_subscription_group_id_fkey;

GRANT UPDATE, DELETE ON beckett.subscriptions TO beckett;

-- Step 8: Create new checkpoints table with optimal column ordering
CREATE TABLE beckett.checkpoints_new
(
  id bigint GENERATED ALWAYS AS IDENTITY,
  subscription_id bigint NOT NULL,
  stream_version bigint NOT NULL DEFAULT 0,
  stream_position bigint NOT NULL DEFAULT 0,
  created_at timestamp with time zone DEFAULT now(),
  updated_at timestamp with time zone DEFAULT now(),
  process_at timestamp with time zone NULL,
  reserved_until timestamp with time zone NULL,
  retry_attempts int NOT NULL DEFAULT 0,
  lagging boolean GENERATED ALWAYS AS (stream_version > stream_position) STORED,
  status beckett.checkpoint_status NOT NULL DEFAULT 'active',
  stream_name text NOT NULL,
  retries beckett.retry[] NULL
);

-- Step 9: Populate new checkpoints table in batches
DO $$
DECLARE
    batch_size INT := 10000;
    affected_rows INT;
    total_migrated BIGINT := 0;
    start_time TIMESTAMP;
    last_id BIGINT := 0;
BEGIN
    start_time := clock_timestamp();
    RAISE NOTICE 'Starting checkpoints migration at %', start_time;

    -- First check total rows to migrate
    SELECT count(*) INTO affected_rows
    FROM beckett.checkpoints;

    RAISE NOTICE 'Found % checkpoints to migrate', affected_rows;

    IF affected_rows = 0 THEN
        RAISE NOTICE 'No checkpoints to migrate';
        RETURN;
    END IF;

    LOOP
        -- Migrate in batches using ID range to avoid re-scanning
        INSERT INTO beckett.checkpoints_new (
            subscription_id, stream_version, stream_position, created_at, updated_at,
            process_at, reserved_until, retry_attempts, status, stream_name, retries
        )
        SELECT 
            s.id as subscription_id,
            c.stream_version, c.stream_position, c.created_at, c.updated_at,
            c.process_at, c.reserved_until, c.retry_attempts, c.status, c.stream_name, c.retries
        FROM beckett.checkpoints c
        INNER JOIN beckett.subscriptions s ON c.group_name = s.group_name AND c.name = s.name
        INNER JOIN beckett.subscription_groups sg ON s.subscription_group_id = sg.id AND sg.name = c.group_name
        WHERE c.id > last_id
        AND c.id IN (
            SELECT c2.id
            FROM beckett.checkpoints c2
            WHERE c2.id > last_id
            ORDER BY c2.id
            LIMIT batch_size
        );

        GET DIAGNOSTICS affected_rows = ROW_COUNT;
        total_migrated := total_migrated + affected_rows;

        -- Get the highest ID we just processed
        SELECT max(c.id) INTO last_id
        FROM beckett.checkpoints c
        WHERE c.id > last_id
        AND EXISTS (
            SELECT 1 FROM beckett.checkpoints_new cn 
            WHERE cn.stream_name = c.stream_name
        );

        RAISE NOTICE 'Batch completed: % rows migrated (total: %, last_id: %, elapsed: %)',
            affected_rows, total_migrated, last_id, clock_timestamp() - start_time;

        -- Exit when no more rows to migrate
        EXIT WHEN affected_rows = 0;

        -- Small pause to avoid overwhelming the database
        PERFORM pg_sleep(0.1);
    END LOOP;

    RAISE NOTICE 'Completed checkpoints migration. Total migrated: %, elapsed: %',
        total_migrated, clock_timestamp() - start_time;
END $$;

-- Step 10: Add constraints to new checkpoints table
ALTER TABLE beckett.checkpoints_new ADD CONSTRAINT checkpoints_new_pkey PRIMARY KEY (id);
ALTER TABLE beckett.checkpoints_new ADD CONSTRAINT checkpoints_new_subscription_id_stream_name_key UNIQUE (subscription_id, stream_name);
ALTER TABLE beckett.checkpoints_new ADD CONSTRAINT checkpoints_new_subscription_id_fkey
    FOREIGN KEY (subscription_id) REFERENCES beckett.subscriptions(id) ON DELETE CASCADE;

-- Step 11: Drop old checkpoints table and rename new one
DROP TABLE beckett.checkpoints CASCADE;
ALTER TABLE beckett.checkpoints_new RENAME TO checkpoints;

-- Rename constraints to remove "new" suffix
ALTER TABLE beckett.checkpoints RENAME CONSTRAINT checkpoints_new_pkey TO checkpoints_pkey;
ALTER TABLE beckett.checkpoints RENAME CONSTRAINT checkpoints_new_subscription_id_stream_name_key TO checkpoints_subscription_id_stream_name_key;
ALTER TABLE beckett.checkpoints RENAME CONSTRAINT checkpoints_new_subscription_id_fkey TO checkpoints_subscription_id_fkey;

GRANT UPDATE, DELETE ON beckett.checkpoints TO beckett;

-- Step 12: Create indexes on new tables
CREATE INDEX ix_subscriptions_reservation_candidates ON beckett.subscriptions (subscription_group_id, name, status)
WHERE status = 'active' OR status = 'replay';

CREATE INDEX ix_checkpoints_to_process ON beckett.checkpoints (subscription_id, process_at, reserved_until)
WHERE process_at IS NOT NULL AND reserved_until IS NULL;

CREATE INDEX ix_checkpoints_reserved ON beckett.checkpoints (subscription_id, reserved_until)
WHERE reserved_until IS NOT NULL;

CREATE INDEX ix_checkpoints_metrics ON beckett.checkpoints (status, lagging, subscription_id);

CREATE INDEX ix_checkpoints_subscription_id ON beckett.checkpoints (subscription_id);

-- Step 13: Add checkpoint preprocessor trigger
CREATE TRIGGER checkpoint_preprocessor BEFORE INSERT OR UPDATE ON beckett.checkpoints
  FOR EACH ROW EXECUTE FUNCTION beckett.checkpoint_preprocessor();

-- Step 14: Update checkpoint type definition
DROP TYPE IF EXISTS beckett.checkpoint CASCADE;
CREATE TYPE beckett.checkpoint AS
(
  subscription_id bigint,
  stream_name text,
  stream_version bigint,
  stream_position bigint
);

-- Step 12: Update checkpoint preprocessor function to use new schema
CREATE OR REPLACE FUNCTION beckett.checkpoint_preprocessor()
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

-- Step 13: Update utility functions to use new schema
CREATE OR REPLACE FUNCTION beckett.delete_subscription(
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
  FROM beckett.subscriptions s
  INNER JOIN beckett.subscription_groups sg ON s.subscription_group_id = sg.id
  WHERE sg.name = _group_name
  AND s.name = _name;

  IF _subscription_id IS NOT NULL THEN
    LOOP
      DELETE FROM beckett.checkpoints
      WHERE id IN (
        SELECT id
        FROM beckett.checkpoints
        WHERE subscription_id = _subscription_id
        LIMIT 500
      );

      GET DIAGNOSTICS _rows_deleted = ROW_COUNT;
      EXIT WHEN _rows_deleted = 0;
    END LOOP;

    DELETE FROM beckett.subscriptions WHERE id = _subscription_id;
  END IF;
END;
$$;

CREATE OR REPLACE FUNCTION beckett.move_subscription(
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
  FROM beckett.subscriptions s
  INNER JOIN beckett.subscription_groups sg ON s.subscription_group_id = sg.id
  WHERE sg.name = _group_name
  AND s.name = _name;

  SELECT id INTO _new_subscription_group_id
  FROM beckett.subscription_groups
  WHERE name = _new_group_name;

  IF _subscription_id IS NOT NULL AND _new_subscription_group_id IS NOT NULL THEN
    UPDATE beckett.subscriptions
    SET subscription_group_id = _new_subscription_group_id
    WHERE id = _subscription_id;
  END IF;
END;
$$;

CREATE OR REPLACE FUNCTION beckett.rename_subscription(
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
  FROM beckett.subscriptions s
  INNER JOIN beckett.subscription_groups sg ON s.subscription_group_id = sg.id
  WHERE sg.name = _group_name
  AND s.name = _name;

  IF _subscription_id IS NOT NULL THEN
    UPDATE beckett.subscriptions
    SET name = _new_name
    WHERE id = _subscription_id;
  END IF;
END;
$$;

CREATE OR REPLACE FUNCTION beckett.replay_subscription(
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
  FROM beckett.subscriptions s
  INNER JOIN beckett.subscription_groups sg ON s.subscription_group_id = sg.id
  WHERE sg.name = _group_name
  AND s.name = _name;

  IF _subscription_id IS NOT NULL THEN
    SELECT coalesce(max(m.global_position), 0)
    INTO _replay_target_position
    FROM beckett.checkpoints c
    INNER JOIN beckett.messages_active m ON c.stream_name = m.stream_name AND c.stream_version = m.stream_position
    WHERE c.subscription_id = _subscription_id;

    LOOP
      UPDATE beckett.checkpoints
      SET stream_position = 0
      WHERE id IN (
        SELECT id
        FROM beckett.checkpoints
        WHERE subscription_id = _subscription_id
        AND stream_position > 0
        LIMIT 500
      );

      GET DIAGNOSTICS _rows_updated = ROW_COUNT;
      EXIT WHEN _rows_updated = 0;
    END LOOP;

    UPDATE beckett.subscriptions
    SET status = 'replay',
        replay_target_position = _replay_target_position
    WHERE id = _subscription_id;
  END IF;
END;
$$;

CREATE OR REPLACE FUNCTION beckett.reset_subscription(
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
  FROM beckett.subscriptions s
  INNER JOIN beckett.subscription_groups sg ON s.subscription_group_id = sg.id
  WHERE sg.name = _group_name
  AND s.name = _name;

  IF _subscription_id IS NOT NULL THEN
    LOOP
      DELETE FROM beckett.checkpoints
      WHERE subscription_id = _subscription_id
      AND id IN (
        SELECT id FROM beckett.checkpoints
        WHERE subscription_id = _subscription_id
        LIMIT 500
      );

      GET DIAGNOSTICS _rows_deleted = ROW_COUNT;
      EXIT WHEN _rows_deleted = 0;
    END LOOP;

    UPDATE beckett.subscriptions
    SET status = 'uninitialized',
        replay_target_position = NULL
    WHERE id = _subscription_id;

    INSERT INTO beckett.checkpoints (subscription_id, stream_name)
    VALUES (_subscription_id, '$initializing')
    ON CONFLICT (subscription_id, stream_name) DO UPDATE
      SET stream_version = 0,
          stream_position = 0;
  END IF;
END;
$$;

-- Step 15: Data migration complete - new tables with optimal column ordering are now in place
-- subscription_groups: uses existing table with id, name
-- subscriptions: replaced with optimal ordering - id, subscription_group_id, name, status, replay_target_position
-- checkpoints: replaced with optimal ordering - id, subscription_id, stream_version, stream_position, created_at, updated_at, process_at, reserved_until, retry_attempts, lagging, status, stream_name, retries
