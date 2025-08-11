-- Beckett v0.23.5 - Remove stream_version from checkpoints table

-- The target stream version is now tracked in checkpoints_ready table
-- This creates a cleaner "presence-based" model where:
-- - checkpoints table only tracks current position
-- - checkpoints_ready presence indicates work needed up to target_stream_version

ALTER TABLE beckett.checkpoints DROP COLUMN IF EXISTS stream_version;

-- Note: Lagging detection is now simply COUNT(*) FROM checkpoints_ready