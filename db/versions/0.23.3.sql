-- Beckett v0.23.3 - Remove process_at and reserved_until from checkpoints table

-- These columns are now redundant as the state is managed entirely by 
-- checkpoints_ready and checkpoints_reserved tables

-- Step 1: Drop the columns from checkpoints table
ALTER TABLE beckett.checkpoints DROP COLUMN IF EXISTS process_at;
ALTER TABLE beckett.checkpoints DROP COLUMN IF EXISTS reserved_until;
ALTER TABLE beckett.checkpoints DROP COLUMN IF EXISTS lagging;

-- Step 2: Drop any remaining indexes that referenced these columns  
DROP INDEX IF EXISTS ix_checkpoints_to_process;
DROP INDEX IF EXISTS ix_checkpoints_reserved;
DROP INDEX IF EXISTS ix_checkpoints_metrics; -- This likely includes lagging column

-- Step 3: Recreate metrics index without lagging column
CREATE INDEX ix_checkpoints_metrics ON beckett.checkpoints (status, subscription_id);

-- Note: 
-- - checkpoints_ready and checkpoints_reserved tables now serve as the single source of truth for checkpoint scheduling and reservation state
-- - lagging is now computed as stream_version > stream_position directly in queries (more efficient than stored column)