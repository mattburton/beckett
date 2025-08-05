-- Beckett v0.20.13 - fix correlation ID query performance issue

DROP INDEX IF EXISTS beckett.ix_messages_active_correlation_id;

CREATE INDEX ix_messages_active_correlation_id_global_position
  ON beckett.messages_active ((metadata ->> '$correlation_id'::text), global_position)
  WHERE ((metadata ->> '$correlation_id'::text) IS NOT NULL);
