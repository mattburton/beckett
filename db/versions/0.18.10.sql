-- Beckett v0.18.10 - subscription initialization fixes and improvements
ALTER TYPE beckett.checkpoint ADD ATTRIBUTE stream_position bigint;

CREATE OR REPLACE FUNCTION beckett.get_checkpoint_stream_version(
  _group_name text,
  _name text,
  _stream_name text
)
  RETURNS bigint
  LANGUAGE sql
AS
$$
SELECT stream_version
FROM beckett.checkpoints
WHERE group_name = _group_name
AND name = _name
AND stream_name = _stream_name;
$$;

CREATE OR REPLACE FUNCTION beckett.record_checkpoints(
  _checkpoints beckett.checkpoint[]
)
  RETURNS void
  LANGUAGE sql
AS
$$
INSERT INTO beckett.checkpoints (stream_version, stream_position, group_name, name, stream_name)
SELECT c.stream_version, c.stream_position, c.group_name, c.name, c.stream_name
FROM unnest(_checkpoints) c
ON CONFLICT (group_name, name, stream_name) DO UPDATE
  SET stream_version = excluded.stream_version;
$$;

DROP FUNCTION IF EXISTS beckett.read_global_stream_checkpoint_data(bigint, int);

CREATE OR REPLACE FUNCTION beckett.read_index_batch(
  _starting_global_position bigint,
  _batch_size int,
  _category text DEFAULT NULL,
  _types text[] DEFAULT NULL
)
  RETURNS TABLE (
    stream_name text,
    stream_position bigint,
    global_position bigint,
    type text,
    tenant text,
    "timestamp" timestamp with time zone
  )
  LANGUAGE plpgsql
AS
$$
DECLARE
  _transaction_id xid8;
  _ending_global_position bigint;
BEGIN
  SELECT m.transaction_id
  INTO _transaction_id
  FROM beckett.messages m
  WHERE m.global_position = _starting_global_position
  AND m.archived = false;

  IF (_transaction_id IS NULL) THEN
    _transaction_id = '0'::xid8;
  END IF;

  _ending_global_position = _starting_global_position + _batch_size;

  RETURN QUERY
    SELECT m.stream_name,
           m.stream_position,
           m.global_position,
           m.type,
           m.metadata ->> '$tenant',
           m.timestamp
    FROM beckett.messages m
    WHERE (m.transaction_id, m.global_position) > (_transaction_id, _starting_global_position)
    AND (m.global_position > _starting_global_position AND m.global_position <= _ending_global_position)
    AND m.transaction_id < pg_snapshot_xmin(pg_current_snapshot())
    AND m.archived = false
    AND (_category IS NULL OR beckett.stream_category(m.stream_name) = _category)
    AND (_types IS NULL OR m.type = ANY(_types))
    ORDER BY m.transaction_id, m.global_position
    LIMIT _batch_size;
END;
$$;
