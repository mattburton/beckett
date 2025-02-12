-- support archiving and truncating streams
CREATE FUNCTION beckett.stream_operations()
  RETURNS trigger
  LANGUAGE plpgsql
AS
$$
BEGIN
  IF NEW.type = '$stream_truncated' THEN
    UPDATE beckett.messages
    SET archived = TRUE
    WHERE stream_name = NEW.stream_name
    AND stream_position < NEW.stream_position
    AND archived = FALSE;
  END IF;

  IF NEW.type = '$stream_archived' THEN
    UPDATE beckett.messages
    SET archived = TRUE
    WHERE stream_name = NEW.stream_name
    AND stream_position < NEW.stream_position
    AND archived = FALSE;
  END IF;

  RETURN NEW;
END;
$$;

CREATE TRIGGER stream_operations
  BEFORE INSERT
  ON beckett.messages
  FOR EACH ROW
EXECUTE FUNCTION beckett.stream_operations();
