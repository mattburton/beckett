-- Beckett v0.20.5 - replay updates
CREATE TYPE beckett.stream_message AS
(
  id uuid,
  stream_name text,
  stream_position bigint,
  global_position bigint,
  type text,
  data jsonb,
  metadata jsonb,
  timestamp timestamp with time zone
);

CREATE TYPE beckett.read_global_stream_result AS
(
  messages beckett.stream_message[],
  ending_global_position bigint
);

DROP FUNCTION beckett.read_global_stream(bigint, int, text, text[]);

CREATE OR REPLACE FUNCTION beckett.read_global_stream(
  _starting_global_position bigint,
  _count int,
  _category text DEFAULT NULL,
  _types text[] DEFAULT NULL
)
  RETURNS beckett.read_global_stream_result
  LANGUAGE plpgsql
AS
$$
DECLARE
  _transaction_id xid8;
  _result beckett.read_global_stream_result;
BEGIN
  SELECT m.transaction_id
  INTO _transaction_id
  FROM beckett.messages m
  WHERE m.global_position = _starting_global_position
  AND m.archived = false;

  IF (_transaction_id IS NULL) THEN
    _transaction_id = '0'::xid8;
  END IF;

  WITH batch AS (
    SELECT m.id, m.global_position, m.transaction_id
    FROM beckett.messages m
    WHERE (m.transaction_id, m.global_position) > (_transaction_id, _starting_global_position)
    AND m.transaction_id < pg_snapshot_xmin(pg_current_snapshot())
    AND m.archived = false
    ORDER BY m.transaction_id, m.global_position
    LIMIT _count
  ),
  ending_global_position AS (
    SELECT b.global_position
    FROM batch b
    ORDER BY b.transaction_id DESC, b.global_position DESC
    LIMIT 1
  ),
  results AS (
    SELECT m.id,
           m.stream_name,
           m.stream_position,
           m.global_position,
           m.type,
           m.data,
           m.metadata,
           m.timestamp
    FROM beckett.messages m
    INNER JOIN batch b ON m.id = b.id
    WHERE m.archived = FALSE
    AND (_category IS NULL OR beckett.stream_category(m.stream_name) = _category)
    AND (_types IS NULL OR m.type = ANY (_types))
    ORDER BY m.transaction_id, m.global_position
  )
  SELECT array_agg(r.*), (SELECT global_position FROM ending_global_position)
  INTO _result.messages, _result.ending_global_position
  FROM results r;

  IF (_result.messages IS NULL) THEN
    _result.messages := ARRAY[]::beckett.stream_message[];
  END IF;

  RETURN _result;
END;
$$;