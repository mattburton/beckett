-- Beckett v0.20.12
 
-- inline append query
DROP FUNCTION IF EXISTS beckett.append_to_stream(text, bigint, beckett.message[]);

-- utility function to enable raising exceptions from inline queries
CREATE OR REPLACE FUNCTION beckett.assert_condition(
  _condition boolean,
  _message text
)
  RETURNS boolean
  IMMUTABLE
  LANGUAGE plpgsql
AS
$$
BEGIN
  IF NOT _condition THEN
    RAISE EXCEPTION '%', _message;
  END IF;
  RETURN TRUE;
END;
$$;

-- correlated message query performance fix
DROP INDEX IF EXISTS beckett.ix_messages_active_correlation_id;

CREATE INDEX ix_messages_active_correlation_id_global_position
  ON beckett.messages_active ((metadata ->> '$correlation_id'::text), global_position)
  WHERE ((metadata ->> '$correlation_id'::text) IS NOT NULL);
