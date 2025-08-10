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

-- Step 3: Add new columns to subscriptions table
ALTER TABLE beckett.subscriptions
ADD COLUMN IF NOT EXISTS id bigint GENERATED ALWAYS AS IDENTITY,
ADD COLUMN IF NOT EXISTS subscription_group_id bigint;

-- Step 4: Populate subscription_group_id foreign keys
UPDATE beckett.subscriptions
SET subscription_group_id = sg.id
FROM beckett.subscription_groups sg
WHERE beckett.subscriptions.group_name = sg.name;

-- Step 5: Add foreign key constraint
ALTER TABLE beckett.subscriptions
ADD CONSTRAINT fk_subscriptions_subscription_group_id
FOREIGN KEY (subscription_group_id) REFERENCES beckett.subscription_groups(id) ON DELETE CASCADE;

-- Step 5a: Update unique constraints for subscriptions table
ALTER TABLE beckett.subscriptions DROP CONSTRAINT IF EXISTS subscriptions_group_name_name_key;
ALTER TABLE beckett.subscriptions ADD CONSTRAINT subscriptions_subscription_group_id_name_key UNIQUE (subscription_group_id, name);

-- Step 5b: Ensure $global subscriptions exist for all groups that have checkpoints with name = '$global'
INSERT INTO beckett.subscriptions (subscription_group_id, group_name, name, status)
SELECT DISTINCT sg.id, sg.name, '$global', 'active'::beckett.subscription_status
FROM beckett.subscription_groups sg
WHERE EXISTS (
    SELECT 1
    FROM beckett.checkpoints c
    WHERE c.group_name = sg.name
    AND c.name = '$global'
)
ON CONFLICT (subscription_group_id, name) DO UPDATE SET status = 'active';

-- Step 6: Add new subscription_id column to checkpoints
ALTER TABLE beckett.checkpoints
ADD COLUMN IF NOT EXISTS subscription_id bigint;

-- Step 7: Populate subscription_id foreign keys in batches
DO $$
DECLARE
    batch_size INT := 10000;
    affected_rows INT;
    total_updated BIGINT := 0;
    start_time TIMESTAMP;
    last_id BIGINT := 0;
BEGIN
    start_time := clock_timestamp();
    RAISE NOTICE 'Starting subscription_id population at %', start_time;

    -- First check if we have any rows to update
    SELECT count(*) INTO affected_rows
    FROM beckett.checkpoints
    WHERE subscription_id IS NULL;

    RAISE NOTICE 'Found % checkpoints needing subscription_id population', affected_rows;

    IF affected_rows = 0 THEN
        RAISE NOTICE 'No checkpoints need subscription_id population';
        RETURN;
    END IF;

    -- Report on $global checkpoints specifically
    SELECT count(*) INTO affected_rows
    FROM beckett.checkpoints
    WHERE subscription_id IS NULL
    AND name = '$global';

    RAISE NOTICE 'Found % $global checkpoints that will be migrated', affected_rows;

    LOOP
        -- Update in batches using ID range to avoid re-scanning
        UPDATE beckett.checkpoints c
        SET subscription_id = s.id
        FROM beckett.subscriptions s
        WHERE c.subscription_id IS NULL
        AND c.id > last_id
        AND c.group_name = s.group_name
        AND c.name = s.name
        AND c.id IN (
            SELECT c2.id
            FROM beckett.checkpoints c2
            WHERE c2.subscription_id IS NULL
            AND c2.id > last_id
            ORDER BY c2.id
            LIMIT batch_size
        );

        GET DIAGNOSTICS affected_rows = ROW_COUNT;
        total_updated := total_updated + affected_rows;

        -- Get the highest ID we just processed
        SELECT max(c.id) INTO last_id
        FROM beckett.checkpoints c
        WHERE c.subscription_id IS NOT NULL
        AND c.id > last_id;

        RAISE NOTICE 'Batch completed: % rows updated (total: %, last_id: %, elapsed: %)',
            affected_rows, total_updated, last_id, clock_timestamp() - start_time;

        -- Exit when no more rows to update
        EXIT WHEN affected_rows = 0;

        -- Small pause to avoid overwhelming the database
        PERFORM pg_sleep(0.1);
    END LOOP;

    RAISE NOTICE 'Completed subscription_id population. Total updated: %, elapsed: %',
        total_updated, clock_timestamp() - start_time;
END $$;

-- Step 8: Add primary key to subscriptions table first
ALTER TABLE beckett.subscriptions DROP CONSTRAINT IF EXISTS subscriptions_pkey;
ALTER TABLE beckett.subscriptions ADD CONSTRAINT subscriptions_pkey PRIMARY KEY (id);

-- Step 8a: Make subscription_id NOT NULL now that all values are populated
ALTER TABLE beckett.checkpoints
ALTER COLUMN subscription_id SET NOT NULL;

-- Step 8b: Add foreign key constraint for checkpoints
ALTER TABLE beckett.checkpoints
ADD CONSTRAINT fk_checkpoints_subscription_id
FOREIGN KEY (subscription_id) REFERENCES beckett.subscriptions(id) ON DELETE CASCADE;

-- Step 9: Update indexes (drop old ones, create new ones)
DROP INDEX IF EXISTS beckett.ix_subscriptions_reservation_candidates;
DROP INDEX IF EXISTS beckett.ix_checkpoints_to_process;
DROP INDEX IF EXISTS beckett.ix_checkpoints_reserved;
DROP INDEX IF EXISTS beckett.ix_checkpoints_metrics;

CREATE INDEX ix_subscriptions_reservation_candidates ON beckett.subscriptions (subscription_group_id, name, status)
WHERE status = 'active' OR status = 'replay';

CREATE INDEX IF NOT EXISTS ix_checkpoints_to_process ON beckett.checkpoints (subscription_id, process_at, reserved_until)
WHERE process_at IS NOT NULL AND reserved_until IS NULL;

CREATE INDEX IF NOT EXISTS ix_checkpoints_reserved ON beckett.checkpoints (subscription_id, reserved_until)
WHERE reserved_until IS NULL;

CREATE INDEX IF NOT EXISTS ix_checkpoints_metrics ON beckett.checkpoints (status, lagging, subscription_id);

CREATE INDEX IF NOT EXISTS ix_checkpoints_subscription_id ON beckett.checkpoints (subscription_id);

-- Step 10: Update unique constraints
ALTER TABLE beckett.checkpoints DROP CONSTRAINT IF EXISTS checkpoints_group_name_name_stream_name_key;
ALTER TABLE beckett.checkpoints ADD CONSTRAINT checkpoints_subscription_id_stream_name_key UNIQUE (subscription_id, stream_name);

-- Step 11: Update checkpoint type definition
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

-- Step 14: Clean up - remove old columns after verifying data integrity
ALTER TABLE beckett.subscriptions DROP COLUMN IF EXISTS group_name;
ALTER TABLE beckett.checkpoints DROP COLUMN IF EXISTS group_name;
ALTER TABLE beckett.checkpoints DROP COLUMN IF EXISTS name;
