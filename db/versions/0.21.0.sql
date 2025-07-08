-- Beckett v0.21.0 - rework checkpoint handling - separate ready / reserved tables

ALTER TYPE beckett.checkpoint DROP ATTRIBUTE stream_version;

DROP INDEX IF EXISTS beckett.ix_checkpoints_to_process;
DROP INDEX IF EXISTS beckett.ix_checkpoints_reserved;
DROP INDEX IF EXISTS beckett.ix_checkpoints_metrics;

DROP TRIGGER IF EXISTS checkpoint_preprocessor ON beckett.checkpoints;
DROP FUNCTION IF EXISTS beckett.checkpoint_preprocessor;

CREATE TABLE IF NOT EXISTS beckett.checkpoints_ready
(
  id bigint NOT NULL PRIMARY KEY REFERENCES beckett.checkpoints (id),
  group_name text NOT NULL,
  process_at timestamp with time zone NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_checkpoints_ready ON beckett.checkpoints_ready (group_name, process_at, id);

GRANT UPDATE, DELETE ON beckett.checkpoints_ready TO beckett;

CREATE TABLE IF NOT EXISTS beckett.checkpoints_reserved
(
  id bigint NOT NULL PRIMARY KEY REFERENCES beckett.checkpoints (id),
  group_name text NOT NULL,
  reserved_until timestamp with time zone NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_checkpoints_reserved ON beckett.checkpoints_reserved (group_name, reserved_until, id);

GRANT UPDATE, DELETE ON beckett.checkpoints_reserved TO beckett;

INSERT INTO beckett.checkpoints_ready (id, group_name, process_at)
SELECT id, group_name, process_at
FROM beckett.checkpoints
WHERE process_at IS NOT NULL;

INSERT INTO beckett.checkpoints_reserved (id, group_name, reserved_until)
SELECT id, group_name, reserved_until
FROM beckett.checkpoints
WHERE reserved_at IS NOT NULL;

ALTER TABLE beckett.checkpoints
  DROP COLUMN stream_version,
  DROP COLUMN process_at,
  DROP COLUMN reserved_until,
  DROP COLUMN lagging;

ALTER TABLE beckett.checkpoints
  ADD COLUMN retry_attempts int GENERATED ALWAYS AS (coalesce(array_length(retries, 1), 0)) STORED;
