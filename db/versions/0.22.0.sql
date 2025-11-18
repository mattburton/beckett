-- Beckett v0.22.0 - checkpoint hot tables to reduce contention

DROP INDEX IF EXISTS beckett.idx_subscriptions_checkpoint_reservations;
DROP TRIGGER IF EXISTS checkpoint_preprocessor ON beckett.checkpoints;
DROP FUNCTION IF EXISTS beckett.checkpoint_preprocessor();

CREATE TABLE IF NOT EXISTS beckett.checkpoints_ready
(
  checkpoint_id bigint PRIMARY KEY NOT NULL,
  process_at timestamp with time zone NOT NULL DEFAULT now(),
  group_name text NOT NULL,
  name text NOT NULL
);

GRANT UPDATE, DELETE ON beckett.checkpoints_ready TO beckett;

CREATE TABLE IF NOT EXISTS beckett.checkpoints_reserved
(
  checkpoint_id bigint PRIMARY KEY NOT NULL,
  reserved_until timestamp with time zone NOT NULL,
  group_name text NOT NULL
);

GRANT UPDATE, DELETE ON beckett.checkpoints_reserved TO beckett;

CREATE INDEX IF NOT EXISTS ix_checkpoints_ready ON beckett.checkpoints_ready (group_name, name, process_at)
  INCLUDE (checkpoint_id);

CREATE INDEX IF NOT EXISTS ix_checkpoints_reserved ON beckett.checkpoints_reserved (group_name, reserved_until)
  INCLUDE (checkpoint_id);

CREATE OR REPLACE FUNCTION beckett.checkpoint_ready_notification()
  RETURNS trigger
  LANGUAGE plpgsql
AS
$$
BEGIN
  PERFORM pg_notify('beckett:checkpoints', NEW.group_name);

  RETURN NEW;
END;
$$;

CREATE TRIGGER checkpoint_ready_notification BEFORE INSERT OR UPDATE ON beckett.checkpoints_ready
  FOR EACH ROW EXECUTE FUNCTION beckett.checkpoint_ready_notification();

-- populate existing data
INSERT INTO beckett.checkpoints_ready (checkpoint_id, group_name, name, process_at)
SELECT id, group_name, name, process_at
FROM beckett.checkpoints
WHERE process_at IS NOT NULL;

INSERT INTO beckett.checkpoints_reserved (checkpoint_id, group_name, reserved_until)
SELECT id, group_name, reserved_until
FROM beckett.checkpoints
WHERE reserved_until IS NOT NULL;

-- finish schema changes
DROP INDEX IF EXISTS beckett.ix_checkpoints_reserved;

ALTER TABLE beckett.checkpoints DROP COLUMN process_at;
ALTER TABLE beckett.checkpoints DROP COLUMN reserved_until;
