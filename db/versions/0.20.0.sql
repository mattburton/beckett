-- Beckett v0.20.0 - subscription replay support
DROP FUNCTION IF EXISTS beckett.try_advisory_lock(text);
DROP FUNCTION IF EXISTS beckett.advisory_unlock(text);

ALTER TYPE beckett.subscription_status ADD VALUE 'replay';

ALTER TABLE beckett.subscriptions ADD COLUMN replay_target_position bigint NULL;

CREATE INDEX ix_subscriptions_reservation_candidates ON beckett.subscriptions (group_name, name, status) WHERE status = 'active' OR status = 'replay';

DROP INDEX beckett.ix_subscriptions_active;

DROP FUNCTION IF EXISTS beckett.reserve_next_available_checkpoint(text, interval);

CREATE OR REPLACE FUNCTION beckett.reserve_next_available_checkpoint(
  _group_name text,
  _reservation_timeout interval,
  _reserve_any boolean,
  _reserve_active_only boolean,
  _reserve_replay_only boolean
)
  RETURNS TABLE (
    id bigint,
    group_name text,
    name text,
    stream_name text,
    stream_position bigint,
    stream_version bigint,
    retry_attempts int,
    status beckett.checkpoint_status,
    replay_target_position bigint
  )
  LANGUAGE sql
AS
$$
UPDATE beckett.checkpoints c
SET reserved_until = now() + _reservation_timeout
FROM (
  SELECT c.id, s.replay_target_position
  FROM beckett.checkpoints c
  INNER JOIN beckett.subscriptions s ON c.group_name = s.group_name AND c.name = s.name
  WHERE c.group_name = _group_name
  AND c.process_at <= now()
  AND c.reserved_until IS NULL
  AND (_reserve_any = false OR (s.status = 'active' OR s.status = 'replay'))
  AND (_reserve_active_only = false OR s.status = 'active')
  AND (_reserve_replay_only = false OR s.status = 'replay')
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
  c.status,
  d.replay_target_position;
$$;

CREATE OR REPLACE FUNCTION beckett.replay_subscription(
  _group_name text,
  _name text
)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
BEGIN
  UPDATE beckett.checkpoints
  SET stream_position = 0
  WHERE group_name = _group_name
  AND name = _name;

  WITH global_position AS (
    SELECT stream_position
    FROM beckett.checkpoints
    WHERE group_name = _group_name
    AND name = '$global'
  )
  UPDATE beckett.subscriptions
  SET status = 'replay',
      replay_target_position = global_position.stream_position
  FROM global_position
  WHERE group_name = _group_name
  AND name = _name;
END;
$$;

CREATE OR REPLACE FUNCTION beckett.move_subscription(
  _group_name text,
  _name text,
  _new_group_name text
)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
BEGIN
  UPDATE beckett.subscriptions
  SET group_name = _new_group_name
  WHERE group_name = _group_name
  AND name = _name;

  UPDATE beckett.checkpoints
  SET group_name = _new_group_name
  WHERE group_name = _group_name
  AND name = _name;
END;
$$;

CREATE OR REPLACE FUNCTION beckett.rename_subscription(
  _group_name text,
  _name text,
  _new_name text
)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
BEGIN
  UPDATE beckett.subscriptions
  SET name = _new_name
  WHERE group_name = _group_name
  AND name = _name;

  UPDATE beckett.checkpoints
  SET name = _new_name
  WHERE group_name = _group_name
  AND name = _name;
END;
$$;
