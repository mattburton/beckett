CREATE OR REPLACE FUNCTION beckett.checkpoint_preprocessor() RETURNS trigger
  LANGUAGE plpgsql
AS $$
BEGIN
  IF (TG_OP = 'UPDATE') THEN
    NEW.updated_at = now();
  END IF;

  IF (NEW.status = 'active' AND NEW.process_at IS NULL AND NEW.stream_version > NEW.stream_position) THEN
    NEW.process_at = now();
  END IF;

  IF (NEW.name != '$global' AND NEW.process_at IS NOT NULL) THEN
    PERFORM pg_notify('beckett:checkpoints', NEW.group_name);
  END IF;

  RETURN NEW;
END;
$$;
