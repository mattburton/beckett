-- Beckett v0.23.4 - Add target_stream_version to checkpoints_ready table

-- Step 1: Add target_stream_version column to checkpoints_ready
ALTER TABLE beckett.checkpoints_ready 
ADD COLUMN target_stream_version bigint;

-- Step 2: Populate with current stream_version from checkpoints for existing ready records
UPDATE beckett.checkpoints_ready cr 
SET target_stream_version = c.stream_version
FROM beckett.checkpoints c 
WHERE cr.id = c.id;

-- Step 3: Make the column NOT NULL after population
ALTER TABLE beckett.checkpoints_ready 
ALTER COLUMN target_stream_version SET NOT NULL;

-- Note: This prepares for removing stream_version from the main checkpoints table
-- The ready table will now be the authoritative source for "target" versions