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
    AND stream_position < NEW.stream_position;

    IF FOUND IS FALSE THEN
      RETURN NULL;
    END IF;
  END IF;

  IF NEW.type = '$stream_archived' THEN
    UPDATE beckett.messages
    SET archived = TRUE
    WHERE stream_name = NEW.stream_name
      AND stream_position < NEW.stream_position;

    IF FOUND IS FALSE THEN
      RETURN NULL;
    END IF;
  END IF;

  RETURN new;
END;
$$;

CREATE TRIGGER stream_operations
  BEFORE INSERT
  ON beckett.messages
  FOR EACH ROW
EXECUTE FUNCTION beckett.stream_operations();
