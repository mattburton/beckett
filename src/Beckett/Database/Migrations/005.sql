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

CREATE TYPE __schema__.checkpoint_status AS ENUM (
  'active',
  'lagging',
  'reserved',
  'retry',
  'failed',
  'deleted'
);

CREATE TYPE __schema__.retry_status AS ENUM (
  'started',
  'reserved',
  'scheduled',
  'succeeded',
  'failed',
  'manual_retry_requested',
  'manual_retry_failed',
  'deleted'
);

CREATE TABLE IF NOT EXISTS __schema__.subscriptions
(
  group_name text NOT NULL,
  name text NOT NULL,
  initialized boolean DEFAULT false NOT NULL,
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
  PRIMARY KEY (group_name, name, stream_name)
);

CREATE INDEX IF NOT EXISTS ix_checkpoints_status ON __schema__.checkpoints (status);

CREATE INDEX IF NOT EXISTS ix_checkpoints_reserve_next_available ON __schema__.checkpoints (group_name, name, status) WHERE status = 'lagging';

GRANT UPDATE, DELETE ON __schema__.checkpoints TO beckett;

CREATE TABLE IF NOT EXISTS __schema__.retries
(
  id uuid NOT NULL PRIMARY KEY DEFAULT gen_random_uuid(),
  stream_position bigint NOT NULL DEFAULT 0,
  started_at timestamp with time zone NOT NULL DEFAULT now(),
  retry_at timestamp with time zone NULL,
  reserved_until timestamp with time zone NULL,
  attempts int NULL DEFAULT 0,
  max_retry_count int NULL DEFAULT 0,
  status __schema__.retry_status NOT NULL DEFAULT 'started',
  previous_status __schema__.retry_status NULL,
  group_name text NOT NULL,
  name text NOT NULL,
  stream_name text NOT NULL,
  error jsonb NULL,
  UNIQUE (group_name, name, stream_name, stream_position)
);

CREATE INDEX IF NOT EXISTS ix_retries_status ON __schema__.retries (status);

GRANT UPDATE, DELETE ON __schema__.retries TO beckett;

CREATE TABLE IF NOT EXISTS __schema__.retry_events
(
  id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  retry_id uuid NOT NULL,
  timestamp timestamp with time zone NOT NULL DEFAULT now(),
  attempt int NULL,
  status __schema__.retry_status NOT NULL,
  error jsonb NULL,
  FOREIGN KEY (retry_id) REFERENCES __schema__.retries (id)
);

GRANT UPDATE, DELETE ON __schema__.retry_events TO beckett;

-------------------------------------------------
-- SUBSCRIPTIONS
-------------------------------------------------

CREATE OR REPLACE FUNCTION __schema__.add_or_update_subscription(
  _group_name text,
  _name text
)
  RETURNS TABLE (
    initialized boolean
  )
  LANGUAGE sql
AS
$$
INSERT INTO __schema__.subscriptions (group_name, name)
VALUES (_group_name, _name)
ON CONFLICT (group_name, name) DO NOTHING;

SELECT initialized
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
AND initialized = false
LIMIT 1;
$$;

CREATE OR REPLACE FUNCTION __schema__.set_subscription_to_initialized(
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
SET initialized = true
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

CREATE OR REPLACE FUNCTION __schema__.reserve_checkpoint(
  _group_name text,
  _name text,
  _stream_name text,
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
  AND c.name = _name
  AND c.stream_name = _stream_name
  AND c.status != 'reserved'
  FOR UPDATE SKIP LOCKED
  LIMIT 1
) as d
WHERE c.group_name = d.group_name
AND c.name = d.name
AND c.stream_name = d.stream_name
RETURNING c.group_name, c.name, c.stream_name, c.stream_position, c.stream_version, c.status;
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
  AND s.initialized = true
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
  _status __schema__.checkpoint_status,
  _max_retry_count int DEFAULT NULL,
  _error jsonb DEFAULT NULL
)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
DECLARE
  _retry_id uuid;
BEGIN
  IF (_status = 'retry') THEN
    INSERT INTO __schema__.retries (stream_position, max_retry_count, group_name, name, stream_name, error)
    VALUES (_stream_position, _max_retry_count, _group_name, _name, _stream_name, _error)
    ON CONFLICT (group_name, name, stream_name, stream_position) DO NOTHING
    RETURNING id into _retry_id;

    PERFORM pg_notify('beckett:retries', NULL);
  END IF;

  UPDATE __schema__.checkpoints
  SET stream_position = _stream_position,
      reserved_until = NULL,
      status = CASE
        WHEN (_status = 'active' AND stream_version > _stream_position) THEN
          'lagging'::__schema__.checkpoint_status
        ELSE
          _status
      END,
      previous_status = NULL,
      retry_id = _retry_id
  WHERE group_name = _group_name
  AND name = _name
  AND stream_name = _stream_name;
END;
$$;

-------------------------------------------------
-- RETRIES
-------------------------------------------------

CREATE OR REPLACE FUNCTION __schema__.reserve_next_available_retry(
  _group_name text,
  _reservation_timeout interval
)
  RETURNS TABLE (
    id uuid,
    group_name text,
    name text,
    stream_name text,
    stream_position bigint,
    status __schema__.retry_status,
    attempts int,
    max_retry_count int,
    error jsonb
  )
  LANGUAGE sql
AS
$$
UPDATE __schema__.retries r
SET status = 'reserved',
    previous_status = status,
    reserved_until = now() + _reservation_timeout
FROM (
  SELECT id
  FROM __schema__.retries
  WHERE group_name = _group_name
  AND (
    status = 'started'
    OR
    (status = 'scheduled' AND retry_at <= now() AND attempts < max_retry_count)
    OR
    (status = 'manual_retry_requested' AND retry_at <= now())
    OR
    (status = 'manual_retry_failed' AND attempts < max_retry_count)
  )
  FOR UPDATE SKIP LOCKED
  LIMIT 1
) as d
WHERE r.id = d.id
RETURNING r.id, r.group_name, r.name, r.stream_name, r.stream_position, r.previous_status, r.attempts, r.max_retry_count, r.error;
$$;

CREATE OR REPLACE FUNCTION __schema__.record_retry_event(
  _retry_id uuid,
  _status __schema__.retry_status,
  _attempt int DEFAULT NULL,
  _retry_at timestamp with time zone DEFAULT NULL,
  _error jsonb DEFAULT NULL
)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
BEGIN
  UPDATE __schema__.retries
  SET attempts = COALESCE(_attempt, attempts),
      status = _status,
      previous_status = NULL,
      reserved_until = NULL,
      retry_at = _retry_at
  WHERE id = _retry_id;

  INSERT INTO __schema__.retry_events (retry_id, attempt, status, error)
  VALUES (_retry_id, _attempt, _status, _error);

  IF (_status = 'succeeded') THEN
    UPDATE __schema__.checkpoints
    SET status = CASE
          WHEN (stream_version > stream_position) THEN
            'lagging'::__schema__.checkpoint_status
          ELSE
            'active'::__schema__.checkpoint_status
        END,
        retry_id = NULL
    WHERE retry_id = _retry_id;
  END IF;

  IF (_status = 'failed') THEN
    UPDATE __schema__.checkpoints
    SET status = 'failed'
    WHERE retry_id = _retry_id;
  END IF;

  IF (_status = 'deleted') THEN
    UPDATE __schema__.checkpoints
    SET status = 'deleted'
    WHERE retry_id = _retry_id;
  END IF;

  PERFORM pg_notify('beckett:retries', NULL);
END;
$$;

-------------------------------------------------
-- METRICS
-------------------------------------------------

CREATE OR REPLACE FUNCTION __schema__.get_subscription_lag()
  RETURNS bigint
  LANGUAGE sql
AS
$$
WITH lagging_subscriptions AS (
  SELECT name, group_name, SUM(stream_version - stream_position) AS total_lag
  FROM __schema__.checkpoints
  WHERE status = 'lagging'
  GROUP BY name, group_name
)
SELECT count(*)
FROM lagging_subscriptions;
$$;

CREATE OR REPLACE FUNCTION __schema__.get_subscription_retry_count()
  RETURNS bigint
  LANGUAGE sql
AS
$$
SELECT count(*)
FROM __schema__.checkpoints
WHERE status = 'retry';
$$;

CREATE OR REPLACE FUNCTION __schema__.get_subscription_failed_count()
  RETURNS bigint
  LANGUAGE sql
AS
$$
SELECT count(*)
FROM __schema__.checkpoints
WHERE status = 'failed';
$$;
