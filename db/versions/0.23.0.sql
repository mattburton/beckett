-- Beckett v0.23.0 - optimize checkpoint system with ready/reserved tables

-- Step 1: Create checkpoints_ready table
CREATE TABLE IF NOT EXISTS beckett.checkpoints_ready
(
    id bigint NOT NULL,
    process_at timestamp with time zone NOT NULL DEFAULT now(),
    PRIMARY KEY (id)
);

GRANT SELECT, INSERT, UPDATE, DELETE ON beckett.checkpoints_ready TO beckett;

-- Step 2: Create checkpoints_reserved table  
CREATE TABLE IF NOT EXISTS beckett.checkpoints_reserved
(
    id bigint NOT NULL,
    reserved_until timestamp with time zone NOT NULL,
    PRIMARY KEY (id)
);

GRANT SELECT, INSERT, UPDATE, DELETE ON beckett.checkpoints_reserved TO beckett;

-- Step 3: Add foreign key constraints
ALTER TABLE beckett.checkpoints_ready 
    ADD CONSTRAINT checkpoints_ready_id_fkey 
    FOREIGN KEY (id) REFERENCES beckett.checkpoints(id) ON DELETE CASCADE;

ALTER TABLE beckett.checkpoints_reserved 
    ADD CONSTRAINT checkpoints_reserved_id_fkey 
    FOREIGN KEY (id) REFERENCES beckett.checkpoints(id) ON DELETE CASCADE;

-- Step 4: Create indexes for performance
CREATE INDEX ix_checkpoints_ready_process_at ON beckett.checkpoints_ready (process_at);
CREATE INDEX ix_checkpoints_reserved_reserved_until ON beckett.checkpoints_reserved (reserved_until);

-- Step 5: Populate checkpoints_ready with existing ready checkpoints
INSERT INTO beckett.checkpoints_ready (id, process_at)
SELECT c.id, c.process_at
FROM beckett.checkpoints c
WHERE c.process_at IS NOT NULL 
  AND c.reserved_until IS NULL
ON CONFLICT DO NOTHING;

-- Step 6: Populate checkpoints_reserved with existing reserved checkpoints  
INSERT INTO beckett.checkpoints_reserved (id, reserved_until)
SELECT c.id, c.reserved_until
FROM beckett.checkpoints c
WHERE c.reserved_until IS NOT NULL
ON CONFLICT DO NOTHING;

-- Step 7: Update checkpoint preprocessor to manage new tables
CREATE OR REPLACE FUNCTION beckett.checkpoint_preprocessor()
    RETURNS trigger
    LANGUAGE plpgsql
AS
$$
BEGIN
    IF (TG_OP = 'UPDATE') THEN
        NEW.updated_at = now();
    END IF;

    -- Handle transition to ready state
    IF (NEW.status = 'active' AND NEW.process_at IS NULL AND NEW.stream_version > NEW.stream_position) THEN
        NEW.process_at = now();
    END IF;

    -- Handle ready state changes
    IF (TG_OP = 'INSERT' OR TG_OP = 'UPDATE') THEN
        IF (NEW.process_at IS NOT NULL AND NEW.reserved_until IS NULL AND NEW.status = 'active' AND NEW.stream_version > NEW.stream_position) THEN
            -- Insert or update in ready table
            INSERT INTO beckett.checkpoints_ready (id, process_at)
            VALUES (NEW.id, NEW.process_at)
            ON CONFLICT (id) DO UPDATE SET process_at = EXCLUDED.process_at;
        ELSE
            -- Remove from ready table if not ready
            DELETE FROM beckett.checkpoints_ready WHERE id = NEW.id;
        END IF;
    END IF;

    -- Handle reservation changes (only on UPDATE)
    IF (TG_OP = 'UPDATE') THEN
        IF (OLD.reserved_until IS NULL AND NEW.reserved_until IS NOT NULL) THEN
            -- Checkpoint being reserved - insert into reserved table and remove from ready
            INSERT INTO beckett.checkpoints_reserved (id, reserved_until)
            VALUES (NEW.id, NEW.reserved_until)
            ON CONFLICT DO NOTHING;
            DELETE FROM beckett.checkpoints_ready WHERE id = NEW.id;
        ELSIF (OLD.reserved_until IS NOT NULL AND NEW.reserved_until IS NULL) THEN
            -- Checkpoint being unreserved - remove from reserved table
            DELETE FROM beckett.checkpoints_reserved WHERE id = NEW.id;
        ELSIF (OLD.reserved_until IS NOT NULL AND NEW.reserved_until IS NOT NULL AND OLD.reserved_until != NEW.reserved_until) THEN
            -- Update reservation time
            UPDATE beckett.checkpoints_reserved SET reserved_until = NEW.reserved_until WHERE id = NEW.id;
        END IF;
    END IF;

    IF (NEW.process_at IS NOT NULL AND NEW.reserved_until IS NULL) THEN
        PERFORM pg_notify('beckett:checkpoints', NEW.subscription_id::text);
    END IF;

    RETURN NEW;
END;
$$;