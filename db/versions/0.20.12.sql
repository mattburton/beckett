-- Beckett v0.20.12 - inline append query

DROP FUNCTION IF EXISTS beckett.append_to_stream(text, bigint, beckett.message[]);

-- utility function to enable raising exceptions in an inline query
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
