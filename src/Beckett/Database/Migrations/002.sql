-------------------------------------------------
-- MESSAGE STORE SUPPORT
-------------------------------------------------

CREATE TYPE __schema__.message AS
(
  id uuid,
  stream_name text,
  type text,
  data jsonb,
  metadata jsonb,
  expected_version bigint
);

CREATE OR REPLACE FUNCTION __schema__.stream_category(
  _stream_name text
)
  RETURNS text
  IMMUTABLE
  LANGUAGE sql
AS
$$
SELECT split_part(_stream_name, '-', 1);
$$;

CREATE TABLE IF NOT EXISTS __schema__.messages
(
  id uuid NOT NULL UNIQUE,
  global_position bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  stream_position bigint NOT NULL,
  transaction_id xid8 DEFAULT pg_current_xact_id() NOT NULL,
  timestamp timestamp with time zone DEFAULT now() NOT NULL,
  stream_name text NOT NULL,
  type text NOT NULL,
  data jsonb NOT NULL,
  metadata jsonb NOT NULL,
  UNIQUE (stream_name, stream_position)
);

CREATE INDEX IF NOT EXISTS ix_messages_stream_category ON __schema__.messages (
  __schema__.stream_category(stream_name)
);

CREATE OR REPLACE FUNCTION __schema__.stream_hash(
  _stream_name text
)
  RETURNS bigint
  IMMUTABLE
  LANGUAGE sql
AS
$$
SELECT abs(hashtextextended(_stream_name, 0));
$$;

CREATE OR REPLACE FUNCTION __schema__.append_to_stream(
  _stream_name text,
  _expected_version bigint,
  _messages __schema__.message[]
)
  RETURNS bigint
  LANGUAGE plpgsql
AS
$$
DECLARE
  _current_version bigint;
  _stream_version bigint;
BEGIN
  PERFORM pg_advisory_xact_lock(__schema__.stream_hash(_stream_name));

  SELECT coalesce(max(m.stream_position), 0)
  INTO _current_version
  FROM __schema__.messages m
  WHERE m.stream_name = _stream_name;

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
    INSERT INTO __schema__.messages (
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

CREATE OR REPLACE FUNCTION __schema__.read_stream(
  _stream_name text,
  _starting_stream_position bigint DEFAULT NULL,
  _count integer DEFAULT NULL,
  _read_forwards boolean DEFAULT true
)
  RETURNS TABLE (
    id uuid,
    stream_name text,
    stream_version bigint,
    stream_position bigint,
    global_position bigint,
    type text,
    data text,
    metadata text,
    "timestamp" timestamp with time zone
  )
  LANGUAGE sql
AS
$$
WITH stream_version AS (
  SELECT max(m.stream_position) as stream_version
  FROM __schema__.messages m
  WHERE m.stream_name = _stream_name
)
SELECT m.id,
       m.stream_name,
       sv.stream_version,
       m.stream_position,
       m.global_position,
       m.type,
       m.data,
       m.metadata,
       m.timestamp
FROM stream_version sv, __schema__.messages m
WHERE m.stream_name = _stream_name
AND (_starting_stream_position IS NULL OR m.stream_position >= _starting_stream_position)
ORDER BY CASE WHEN _read_forwards = true THEN stream_position END,
         CASE WHEN _read_forwards = false THEN stream_position END DESC
LIMIT _count;
$$;

CREATE OR REPLACE FUNCTION __schema__.read_global_stream(
  _starting_global_position bigint,
  _batch_size int
)
  RETURNS TABLE (
    stream_name text,
    stream_position bigint,
    global_position bigint,
    type text
  )
  LANGUAGE sql
AS
$$
WITH last_transaction_id AS (
  SELECT transaction_id
  FROM __schema__.messages
  WHERE global_position = _starting_global_position
  UNION ALL
  SELECT '0'::xid8 AS transaction_id
  LIMIT 1
)
SELECT m.stream_name, m.stream_position, m.global_position, m.type
FROM last_transaction_id lti, __schema__.messages m
WHERE (
  (m.transaction_id = lti.transaction_id AND m.global_position > _starting_global_position)
  OR
  (m.transaction_id > lti.transaction_id)
)
AND m.transaction_id < pg_snapshot_xmin(pg_current_snapshot())
ORDER BY m.transaction_id, m.global_position
LIMIT _batch_size;
$$;
