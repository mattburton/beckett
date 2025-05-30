-- Beckett v0.18.5 - add back query specific to reading checkpoint data from global stream, update read global stream to limit number of records read to avoid scans
CREATE OR REPLACE FUNCTION beckett.read_global_stream_checkpoint_data(
  _starting_global_position bigint,
  _batch_size int
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
    SELECT m.stream_name,
           m.stream_position,
           m.global_position,
           m.type,
           m.metadata ->> '$tenant',
           m.timestamp
    FROM beckett.messages m
    WHERE (m.transaction_id, m.global_position) > (_transaction_id, _starting_global_position)
    AND m.transaction_id < pg_snapshot_xmin(pg_current_snapshot())
    AND m.archived = false
    ORDER BY m.transaction_id, m.global_position
    LIMIT _batch_size;
END;
$$;

CREATE OR REPLACE FUNCTION beckett.read_global_stream(
  _starting_global_position bigint,
  _count int,
  _category text DEFAULT NULL,
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

  _ending_global_position = _starting_global_position + _count;

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
    AND (m.global_position > _starting_global_position AND m.global_position <= _ending_global_position)
    AND m.transaction_id < pg_snapshot_xmin(pg_current_snapshot())
    AND m.archived = false
    AND (_category IS NULL OR beckett.stream_category(m.stream_name) = _category)
    AND (_types IS NULL OR m.type = ANY(_types))
    ORDER BY m.transaction_id, m.global_position;
END;
$$;