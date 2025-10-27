-- Beckett v0.21.4 - reinstate append_to_stream function

DROP FUNCTION IF EXISTS beckett.assert_condition;

CREATE OR REPLACE FUNCTION beckett.append_to_stream(
  _stream_name text,
  _expected_version bigint,
  _messages beckett.message[]
)
  RETURNS bigint
  LANGUAGE plpgsql
AS
$$
DECLARE
  _current_version bigint;
  _stream_version bigint;
BEGIN
  IF (_expected_version < 0) THEN
    PERFORM pg_advisory_xact_lock(beckett.stream_hash(_stream_name));
  END IF;

  SELECT coalesce(max(m.stream_position), 0)
  INTO _current_version
  FROM beckett.messages m
  WHERE m.stream_name = _stream_name
  AND m.archived = false;

  IF (_expected_version < -2) THEN
    RAISE EXCEPTION 'Invalid value for expected version: %', _expected_version;
  END IF;

  IF (_expected_version = -1 AND _current_version = 0) THEN
    RAISE EXCEPTION 'Attempted to append to a non-existing stream: %', _stream_name;
  END IF;

  IF (_expected_version = 0 AND _current_version > 0) THEN
    RAISE EXCEPTION 'Attempted to start a stream that already exists: %', _stream_name;
  END IF;

  IF (_expected_version > 0 AND _expected_version != _current_version) THEN
    RAISE EXCEPTION 'Stream % version % does not match expected version %',
      _stream_name,
      _current_version,
      _expected_version;
  END IF;

  WITH append_messages AS (
    INSERT INTO beckett.messages (
      id,
      stream_position,
      stream_name,
      type,
      data,
      metadata
    )
    SELECT m.id,
           _current_version + (row_number() over())::bigint,
           _stream_name,
           m.type,
           m.data,
           m.metadata
    FROM unnest(_messages) AS m
    RETURNING stream_position, type
  )
  SELECT max(stream_position) INTO _stream_version
  FROM append_messages;

  PERFORM pg_notify('beckett:messages', NULL);

  RETURN _stream_version;
END;
$$;
