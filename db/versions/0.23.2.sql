-- Beckett v0.23.2 - Denormalize group name in ready table for performance

-- Step 1: Add subscription_group_name column to checkpoints_ready table
ALTER TABLE beckett.checkpoints_ready
ADD COLUMN subscription_group_name text;

-- Step 2: Populate existing records with group names
UPDATE beckett.checkpoints_ready cr
SET subscription_group_name = sg.name
FROM beckett.checkpoints c
INNER JOIN beckett.subscriptions s ON c.subscription_id = s.id
INNER JOIN beckett.subscription_groups sg ON s.subscription_group_id = sg.id
WHERE cr.id = c.id;

-- Step 3: Make the column NOT NULL after population
ALTER TABLE beckett.checkpoints_ready
ALTER COLUMN subscription_group_name SET NOT NULL;

-- Step 4: Create optimized indexes
-- Drop the old index
DROP INDEX IF EXISTS ix_checkpoints_ready_process_at;

-- Create new compound index for fast group-based lookups
CREATE INDEX ix_checkpoints_ready_group_process_at
ON beckett.checkpoints_ready (subscription_group_name, process_at, id);

-- Note: Cannot create partial index with now() as it's not immutable
-- The compound index above will still provide significant performance benefits
