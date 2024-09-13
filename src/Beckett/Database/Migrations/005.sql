-------------------------------------------------
-- SUBSCRIPTION SUPPORT
-------------------------------------------------

CREATE TYPE __schema__.checkpoint AS
(
  group_name text,
  name text,
  stream_name text,
  stream_version bigint
);

CREATE TYPE __schema__.subscription_status AS ENUM (
  'uninitialized',
  'active'
);

CREATE TYPE __schema__.checkpoint_status AS ENUM (
  'active',
  'lagging',
  'reserved',
  'pending_retry',
  'retry',
  'failed',
  'deleted'
);

CREATE TABLE IF NOT EXISTS __schema__.subscriptions
(
  group_name text NOT NULL,
  name text NOT NULL,
  status __schema__.subscription_status DEFAULT 'uninitialized' NOT NULL,
  PRIMARY KEY (group_name, name)
);

GRANT UPDATE, DELETE ON __schema__.subscriptions TO beckett;

CREATE TABLE IF NOT EXISTS __schema__.checkpoints
(
  stream_version bigint NOT NULL DEFAULT 0,
  stream_position bigint NOT NULL DEFAULT 0,
  reserved_until timestamp with time zone NULL,
  status __schema__.checkpoint_status NOT NULL DEFAULT 'active',
  previous_status __schema__.checkpoint_status NULL,
  group_name text NOT NULL,
  name text NOT NULL,
  stream_name text NOT NULL,
  retry_id uuid NULL,
  error jsonb NULL,
  PRIMARY KEY (group_name, name, stream_name)
);

CREATE INDEX IF NOT EXISTS ix_checkpoints_status ON __schema__.checkpoints (status);

CREATE INDEX IF NOT EXISTS ix_checkpoints_reserve_next_available ON __schema__.checkpoints (group_name, name, status) WHERE status = 'lagging';

GRANT UPDATE, DELETE ON __schema__.checkpoints TO beckett;

-------------------------------------------------
-- SUBSCRIPTIONS
-------------------------------------------------

CREATE OR REPLACE FUNCTION __schema__.add_or_update_subscription(
  _group_name text,
  _name text
)
  RETURNS TABLE (
    status __schema__.subscription_status
  )
  LANGUAGE sql
AS
$$
INSERT INTO __schema__.subscriptions (group_name, name)
VALUES (_group_name, _name)
ON CONFLICT (group_name, name) DO NOTHING;

SELECT status
FROM __schema__.subscriptions
WHERE group_name = _group_name
AND name = _name;
$$;

CREATE OR REPLACE FUNCTION __schema__.get_next_uninitialized_subscription(
  _group_name text
)
  RETURNS TABLE (
    name text
  )
  LANGUAGE sql
AS
$$
SELECT name
FROM __schema__.subscriptions
WHERE group_name = _group_name
AND status = 'uninitialized'
LIMIT 1;
$$;

CREATE OR REPLACE FUNCTION __schema__.set_subscription_to_active(
  _group_name text,
  _name text
)
  RETURNS void
  LANGUAGE sql
AS
$$
DELETE FROM __schema__.checkpoints
WHERE group_name = _group_name
AND name = _name
AND stream_name = '$initializing';

UPDATE __schema__.subscriptions
SET status = 'active'
WHERE group_name = _group_name
AND name = _name;
$$;

-------------------------------------------------
-- CHECKPOINTS
-------------------------------------------------

CREATE OR REPLACE FUNCTION __schema__.ensure_checkpoint_exists(
  _group_name text,
  _name text,
  _stream_name text
)
  RETURNS void
  LANGUAGE sql
AS
$$
INSERT INTO __schema__.checkpoints (group_name, name, stream_name)
VALUES (_group_name, _name, _stream_name)
ON CONFLICT (group_name, name, stream_name) DO NOTHING;
$$;

CREATE OR REPLACE FUNCTION __schema__.lock_checkpoint(
  _group_name text,
  _name text,
  _stream_name text
)
  RETURNS TABLE (
    group_name text,
    name text,
    stream_name text,
    stream_position bigint,
    stream_version bigint,
    status __schema__.checkpoint_status
  )
  LANGUAGE sql
AS
$$
SELECT group_name, name, stream_name, stream_position, stream_version, status
FROM __schema__.checkpoints
WHERE group_name = _group_name
AND name = _name
AND stream_name = _stream_name
FOR UPDATE
SKIP LOCKED;
$$;

CREATE OR REPLACE FUNCTION __schema__.record_checkpoint(
  _group_name text,
  _name text,
  _stream_name text,
  _stream_position bigint,
  _stream_version bigint
)
  RETURNS void
  LANGUAGE sql
AS
$$
INSERT INTO __schema__.checkpoints (stream_version, stream_position, group_name, name, stream_name, status)
VALUES (
  _stream_version,
  _stream_position,
  _group_name, _name,
  _stream_name,
  (CASE WHEN _stream_version > _stream_position THEN 'lagging' ELSE 'active' END)::__schema__.checkpoint_status
)
ON CONFLICT (group_name, name, stream_name) DO UPDATE
  SET status = (CASE WHEN _stream_version > _stream_position THEN 'lagging' ELSE 'active' END)::__schema__.checkpoint_status,
      previous_status = NULL,
      stream_version = excluded.stream_version,
      stream_position = excluded.stream_position;
$$;

CREATE OR REPLACE FUNCTION __schema__.record_checkpoint_error(
  _group_name text,
  _name text,
  _stream_name text,
  _stream_position bigint,
  _error jsonb
)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
BEGIN
  UPDATE __schema__.checkpoints
  SET stream_position = _stream_position,
      reserved_until = NULL,
      status = 'pending_retry',
      previous_status = NULL,
      retry_id = gen_random_uuid(),
      error = _error
  WHERE group_name = _group_name
  AND name = _name
  AND stream_name = _stream_name;

  PERFORM pg_notify('beckett:retries', NULL);
END;
$$;

CREATE OR REPLACE FUNCTION __schema__.record_checkpoints(
  _checkpoints __schema__.checkpoint[]
)
  RETURNS void
  LANGUAGE sql
AS
$$
INSERT INTO __schema__.checkpoints (stream_version, group_name, name, stream_name, status)
SELECT c.stream_version, c.group_name, c.name, c.stream_name, 'lagging'
FROM unnest(_checkpoints) c
ON CONFLICT (group_name, name, stream_name) DO UPDATE
  SET status = (
        CASE
          WHEN checkpoints.status = 'active' AND excluded.stream_version > checkpoints.stream_position
            THEN 'lagging'::__schema__.checkpoint_status
          ELSE checkpoints.status
        END
      ),
      stream_version = excluded.stream_version;

SELECT pg_notify('beckett:checkpoints', _checkpoints[1].group_name);
$$;

CREATE OR REPLACE FUNCTION __schema__.reserve_next_available_checkpoint(
  _group_name text,
  _reservation_timeout interval
)
  RETURNS TABLE (
    group_name text,
    name text,
    stream_name text,
    stream_position bigint,
    stream_version bigint,
    status __schema__.checkpoint_status
  )
  LANGUAGE sql
AS
$$
UPDATE __schema__.checkpoints c
SET status = 'reserved',
    previous_status = status,
    reserved_until = now() + _reservation_timeout
FROM (
  SELECT c.group_name, c.name, c.stream_name
  FROM __schema__.checkpoints c
  INNER JOIN __schema__.subscriptions s ON c.group_name = s.group_name AND c.name = s.name
  WHERE c.group_name = _group_name
  AND c.status = 'lagging'
  AND s.status = 'active'
  LIMIT 1 FOR UPDATE SKIP LOCKED
) as d
WHERE c.group_name = d.group_name
AND c.name = d.name
AND c.stream_name = d.stream_name
RETURNING c.group_name, c.name, c.stream_name, c.stream_position, c.stream_version, c.status;
$$;

CREATE OR REPLACE FUNCTION __schema__.update_checkpoint_status(
  _group_name text,
  _name text,
  _stream_name text,
  _stream_position bigint,
  _status __schema__.checkpoint_status
)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
BEGIN
  UPDATE __schema__.checkpoints
  SET stream_position = _stream_position,
      reserved_until = NULL,
      status = CASE
        WHEN (_status = 'active' AND stream_version > _stream_position) THEN
          'lagging'::__schema__.checkpoint_status
        ELSE
          _status
      END,
      previous_status = NULL
  WHERE group_name = _group_name
  AND name = _name
  AND stream_name = _stream_name;
END;
$$;

-------------------------------------------------
-- RETRIES
-------------------------------------------------

CREATE OR REPLACE FUNCTION __schema__.lock_next_pending_retry(
  _group_name text
)
  RETURNS TABLE (
    group_name text,
    name text,
    stream_name text,
    stream_position bigint,
    retry_id uuid,
    error jsonb
  )
  LANGUAGE sql
AS
$$
SELECT group_name, name, stream_name, stream_position, retry_id, error
FROM __schema__.checkpoints
WHERE group_name = _group_name
AND status = 'pending_retry'
FOR UPDATE
SKIP LOCKED
LIMIT 1;
$$;
