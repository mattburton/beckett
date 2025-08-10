-- Beckett v0.23.1 - Remove checkpoint triggers in favor of manual management

-- Step 1: Drop the checkpoint preprocessor trigger
DROP TRIGGER IF EXISTS checkpoint_preprocessor ON beckett.checkpoints;

-- Step 2: Drop the checkpoint postprocessor trigger
DROP TRIGGER IF EXISTS checkpoint_postprocessor ON beckett.checkpoints;

-- Step 3: Drop the checkpoint trigger functions since they're no longer needed
DROP FUNCTION IF EXISTS beckett.checkpoint_preprocessor();
DROP FUNCTION IF EXISTS beckett.checkpoint_postprocessor();

-- Note: The checkpoints_ready and checkpoints_reserved tables are now managed
-- entirely by the application queries, eliminating trigger overhead.