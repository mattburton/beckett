--NOTE: only need to run if you ran the script for 0.17.1
DROP FUNCTION beckett.reserve_checkpoint(bigint, interval);

DROP FUNCTION beckett.reserve_next_available_checkpoint(text, interval);

CREATE OR REPLACE FUNCTION beckett.reserve_next_available_checkpoint(
  _group_name text,
  _reservation_timeout interval
)
  RETURNS TABLE (
    id bigint,
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
  c.group_name,
  c.name,
  c.stream_name,
  c.stream_position,
  c.stream_version,
  coalesce(array_length(c.retries, 1), 0) as retry_attempts,
  c.status;
$$;

DROP FUNCTION beckett.update_child_checkpoint_position(bigint, bigint, bigint, timestamp with time zone);

DROP FUNCTION beckett.record_checkpoint_stream_retries(bigint, beckett.checkpoint_stream_retry[]);

DROP FUNCTION beckett.get_checkpoint_blocked_streams(bigint, text[]);

ALTER TABLE beckett.checkpoints DROP CONSTRAINT IF EXISTS checkpoints_parent_id_fkey;

DROP INDEX beckett.ix_checkpoints_parent_id;

ALTER TABLE beckett.checkpoints DROP COLUMN IF EXISTS parent_id;

ALTER TABLE beckett.checkpoints ADD COLUMN IF NOT EXISTS parent_id bigint NULL;

DROP TYPE beckett.checkpoint_stream_retry;