ALTER TABLE beckett.checkpoints ADD COLUMN IF NOT EXISTS parent_id bigint NULL;

ALTER TABLE beckett.checkpoints ADD CONSTRAINT checkpoints_parent_id_fkey
FOREIGN KEY (parent_id)
REFERENCES beckett.checkpoints (id)
ON DELETE CASCADE;

CREATE INDEX ix_checkpoints_parent_id ON beckett.checkpoints (parent_id, status, stream_name) WHERE parent_id IS NOT NULL;

CREATE TYPE beckett.checkpoint_stream_retry AS
(
  stream_name text,
  stream_version bigint,
  stream_position bigint,
  error jsonb
);

CREATE OR REPLACE FUNCTION beckett.get_checkpoint_blocked_streams (
  _parent_id bigint,
  _stream_names text[]
)
  RETURNS TABLE(
    stream_name text
  )
  LANGUAGE sql
AS
$$
SELECT stream_name
FROM beckett.checkpoints
WHERE parent_id = _parent_id
AND status IN ('retry', 'failed')
AND stream_name = ANY(_stream_names);
$$;

CREATE OR REPLACE FUNCTION beckett.record_checkpoint_stream_retries (
  _checkpoint_id bigint,
  _retries beckett.checkpoint_stream_retry[]
)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
DECLARE
  _parent_id bigint;
  _parent_group_name text;
  _parent_name text;
BEGIN
SELECT id, group_name, name
INTO _parent_id, _parent_group_name, _parent_name
FROM beckett.checkpoints
WHERE id = _checkpoint_id;

INSERT INTO beckett.checkpoints (parent_id, group_name, name, stream_name, stream_version, stream_position, status, process_at, retries)
SELECT _parent_id,
       _parent_group_name,
       _parent_name,
       r.stream_name,
       r.stream_version,
       r.stream_position,
       'retry',
       now(),
       array[row(0, r.error, now())::beckett.retry]
FROM unnest(_retries) AS r;
END;
$$;

CREATE OR REPLACE FUNCTION beckett.update_child_checkpoint_position(
  _id bigint,
  _stream_position bigint,
  _stream_version bigint,
  _process_at timestamp with time zone
)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
BEGIN
  IF (_process_at IS NOT NULL) THEN
    UPDATE beckett.checkpoints
    SET stream_position = _stream_position,
        stream_version = coalesce(_stream_version, stream_version),
        process_at = _process_at,
        reserved_until = NULL
    WHERE id = _id;
  ELSE
    DELETE FROM beckett.checkpoints
    WHERE id = _id;
  END IF;
END;
$$;

DROP FUNCTION beckett.reserve_next_available_checkpoint(text, interval);

CREATE OR REPLACE FUNCTION beckett.reserve_next_available_checkpoint(
  _group_name text,
  _reservation_timeout interval
)
  RETURNS TABLE (
    id bigint,
    parent_id bigint,
    group_name text,
    name text,
    stream_name text,
    stream_position bigint,
    stream_version bigint,
    retry_attempts int,
    status beckett.checkpoint_status
  )
  LANGUAGE sql
AS
$$
UPDATE beckett.checkpoints c
SET reserved_until = now() + _reservation_timeout
FROM (
  SELECT c.id
  FROM beckett.checkpoints c
  INNER JOIN beckett.subscriptions s ON c.group_name = s.group_name AND c.name = s.name
  WHERE c.group_name = _group_name
  AND c.process_at <= now()
  AND c.reserved_until IS NULL
  AND s.status = 'active'
  ORDER BY c.process_at
  LIMIT 1
  FOR UPDATE
  SKIP LOCKED
) as d
WHERE c.id = d.id
RETURNING
  c.id,
  c.parent_id,
  c.group_name,
  c.name,
  c.stream_name,
  c.stream_position,
  c.stream_version,
  coalesce(array_length(c.retries, 1), 0) as retry_attempts,
  c.status;
$$;

CREATE OR REPLACE FUNCTION beckett.reserve_checkpoint(
  _id bigint,
  _reservation_timeout interval
)
  RETURNS bigint
  LANGUAGE sql
AS
$$
UPDATE beckett.checkpoints c
SET reserved_until = now() + _reservation_timeout
FROM (
  SELECT c.id
  FROM beckett.checkpoints c
  WHERE c.id = _id
  AND c.reserved_until IS NULL
  FOR UPDATE
  SKIP LOCKED
) as d
WHERE c.id = d.id
RETURNING c.stream_version;
$$;