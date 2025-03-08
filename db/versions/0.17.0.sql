-- add types filter to read_stream
DROP FUNCTION beckett.read_stream(
  text,
  bigint,
  bigint,
  bigint,
  bigint,
  integer,
  boolean
);

CREATE OR REPLACE FUNCTION beckett.read_stream(
  _stream_name text,
  _starting_stream_position bigint DEFAULT NULL,
  _ending_stream_position bigint DEFAULT NULL,
  _starting_global_position bigint DEFAULT NULL,
  _ending_global_position bigint DEFAULT NULL,
  _count integer DEFAULT NULL,
  _read_forwards boolean DEFAULT true,
  _types text[] DEFAULT NULL
)
  RETURNS TABLE (
    id uuid,
    stream_name text,
    stream_version bigint,
    stream_position bigint,
    global_position bigint,
    type text,
    data jsonb,
    metadata jsonb,
    "timestamp" timestamp with time zone
  )
  LANGUAGE plpgsql
AS
$$
DECLARE
  _stream_version bigint;
BEGIN
  SELECT max(m.stream_position)
  INTO _stream_version
  FROM beckett.messages m
  WHERE m.stream_name = _stream_name
  AND m.archived = false;

  IF (_stream_version IS NULL) THEN
    _stream_version = 0;
  END IF;

  RETURN QUERY
    SELECT m.id,
           m.stream_name,
           _stream_version as stream_version,
           m.stream_position,
           m.global_position,
           m.type,
           m.data,
           m.metadata,
           m.timestamp
    FROM beckett.messages m
    WHERE m.stream_name = _stream_name
    AND (_starting_stream_position IS NULL OR m.stream_position >= _starting_stream_position)
    AND (_ending_stream_position IS NULL OR m.stream_position <= _ending_stream_position)
    AND m.archived = false
    AND (_starting_global_position IS NULL OR m.global_position >= _starting_global_position)
    AND (_ending_global_position IS NULL OR m.global_position <= _ending_global_position)
    AND (_types IS NULL OR m.type = ANY(_types))
    ORDER BY CASE WHEN _read_forwards = true THEN m.stream_position END,
             CASE WHEN _read_forwards = false THEN m.stream_position END DESC
    LIMIT _count;
END;
$$;

-- replace read_global_stream with a new version that reads the actual stream along with a type filter
DROP FUNCTION beckett.read_global_stream(bigint, int);

CREATE OR REPLACE FUNCTION beckett.read_global_stream(
  _starting_global_position bigint,
  _count int,
  _types text[] DEFAULT NULL
)
  RETURNS TABLE (
    id uuid,
    stream_name text,
    stream_position bigint,
    global_position bigint,
    type text,
    data jsonb,
    metadata jsonb,
    "timestamp" timestamp with time zone
  )
  LANGUAGE plpgsql
AS
$$
DECLARE
  _transaction_id xid8;
BEGIN
  SELECT m.transaction_id
  INTO _transaction_id
  FROM beckett.messages m
  WHERE m.global_position = _starting_global_position
  AND m.archived = false;

  IF (_transaction_id IS NULL) THEN
    _transaction_id = '0'::xid8;
  END IF;

  RETURN QUERY
    SELECT m.id,
           m.stream_name,
           m.stream_position,
           m.global_position,
           m.type,
           m.data,
           m.metadata,
           m.timestamp
    FROM beckett.messages m
    WHERE (m.transaction_id, m.global_position) > (_transaction_id, _starting_global_position)
    AND m.transaction_id < pg_snapshot_xmin(pg_current_snapshot())
    AND m.archived = false
    AND (_types IS NULL OR m.type = ANY(_types))
    ORDER BY m.transaction_id, m.global_position
    LIMIT _count;
END;
$$;

-- update ensure_checkpoint_exists to return the stream_version of the checkpoint
DROP FUNCTION beckett.ensure_checkpoint_exists(text, text, text);

CREATE OR REPLACE FUNCTION beckett.ensure_checkpoint_exists(
  _group_name text,
  _name text,
  _stream_name text
)
  RETURNS bigint
  LANGUAGE sql
AS
$$
WITH new_checkpoint AS (
  INSERT INTO beckett.checkpoints (group_name, name, stream_name)
  VALUES (_group_name, _name, _stream_name)
  ON CONFLICT (group_name, name, stream_name) DO NOTHING
  RETURNING 0 as stream_version
)
SELECT stream_version
FROM beckett.checkpoints
WHERE group_name = _group_name
AND name = _name
AND stream_name = _stream_name
UNION ALL
SELECT stream_version
FROM new_checkpoint;
$$;