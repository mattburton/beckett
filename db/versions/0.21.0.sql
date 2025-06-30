-- Beckett v0.21.0 - drop global subscriptions
DROP FUNCTION beckett.read_global_stream(bigint, int, text, text[]);
DROP FUNCTION beckett.read_index_batch(bigint, int);

CREATE OR REPLACE FUNCTION beckett.read_global_stream(
  _last_global_position bigint,
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
  WHERE m.global_position = _last_global_position
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
    WHERE (m.transaction_id, m.global_position) > (_transaction_id, _last_global_position)
    AND m.transaction_id < pg_snapshot_xmin(pg_current_snapshot())
    AND m.archived = false
    ORDER BY m.transaction_id, m.global_position
    LIMIT _batch_size;
END;
$$;