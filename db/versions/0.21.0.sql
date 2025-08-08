-- Beckett v0.21.0 - recurring message support

CREATE TABLE IF NOT EXISTS beckett.recurring_messages
(
  name text NOT NULL,
  cron_expression text NOT NULL,
  time_zone_id text NOT NULL,
  stream_name text NOT NULL,
  type text NOT NULL,
  data jsonb NOT NULL,
  metadata jsonb NOT NULL,
  next_occurrence timestamp with time zone NULL,
  timestamp timestamp with time zone DEFAULT now() NOT NULL,
  PRIMARY KEY (name)
);

CREATE INDEX IF NOT EXISTS ix_recurring_messages_next_occurrence ON beckett.recurring_messages (next_occurrence ASC)
  WHERE next_occurrence IS NOT NULL;

GRANT UPDATE, DELETE ON beckett.recurring_messages TO beckett;
