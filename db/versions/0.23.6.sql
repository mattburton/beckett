-- Beckett v0.23.6 - Add target_stream_version to checkpoints_reserved table

-- The target_stream_version needs to be carried forward when reserving checkpoints
-- so that expired reservation recovery knows the correct target position

-- Step 1: Add target_stream_version column to checkpoints_reserved
ALTER TABLE beckett.checkpoints_reserved 
ADD COLUMN target_stream_version bigint;

-- Step 2: Populate with reasonable defaults for any existing reserved records
-- (This is unlikely to have data in most cases, but just in case)
UPDATE beckett.checkpoints_reserved cres 
SET target_stream_version = c.stream_position + 1
FROM beckett.checkpoints c 
WHERE cres.id = c.id 
AND cres.target_stream_version IS NULL;

-- Step 3: Make the column NOT NULL after population
ALTER TABLE beckett.checkpoints_reserved 
ALTER COLUMN target_stream_version SET NOT NULL;

-- Note: This ensures expired reservation recovery can restore checkpoints 
-- to the ready table with the correct target version