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
  'retry_pending',
  'retrying',
  'failure_pending',
  'failed',
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
  id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  stream_version bigint DEFAULT 0 NOT NULL,
  stream_position bigint DEFAULT 0 NOT NULL,
  reserved_until timestamp with time zone,
  status __schema__.checkpoint_status DEFAULT 'active' NOT NULL,
  previous_status __schema__.checkpoint_status NULL,
  group_name text NOT NULL,
  name text NOT NULL,
  stream_name text NOT NULL,
  last_error jsonb NULL,
  UNIQUE (group_name, name, stream_name)
);

CREATE INDEX IF NOT EXISTS ix_checkpoints_status ON __schema__.checkpoints (status);

GRANT UPDATE, DELETE ON __schema__.checkpoints TO beckett;

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
    id bigint,
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
SELECT id, group_name, name, stream_name, stream_position, stream_version, status
FROM __schema__.checkpoints
WHERE group_name = _group_name
AND name = _name
AND stream_name = _stream_name
FOR UPDATE
SKIP LOCKED;
$$;

CREATE OR REPLACE FUNCTION __schema__.reserve_checkpoint(
  _id bigint,
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
  SELECT c.id
  FROM __schema__.checkpoints c
  INNER JOIN __schema__.subscriptions s ON c.group_name = s.group_name AND c.name = s.name
  WHERE c.id = _id
  AND c.group_name = _group_name
  AND c.status != 'reserved'
  LIMIT 1 FOR UPDATE SKIP LOCKED
) as d
WHERE c.id = d.id
RETURNING c.id, c.group_name, c.name, c.stream_name, c.stream_position, c.stream_version, c.status;
$$;

CREATE OR REPLACE FUNCTION __schema__.reserve_next_available_checkpoint(
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
  SELECT c.id
  FROM __schema__.checkpoints c
  INNER JOIN __schema__.subscriptions s ON c.group_name = s.group_name AND c.name = s.name
  WHERE c.group_name = _group_name
  AND c.status = 'lagging'
  AND s.initialized = true
  LIMIT 1 FOR UPDATE SKIP LOCKED
) as d
WHERE c.id = d.id
RETURNING c.id, c.group_name, c.name, c.stream_name, c.stream_position, c.stream_version, c.status;
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
  SET status = (CASE WHEN excluded.stream_version > checkpoints.stream_position THEN 'lagging' ELSE 'active' END)::__schema__.checkpoint_status,
      stream_version = excluded.stream_version;

SELECT pg_notify('beckett:checkpoints', _checkpoints[1].group_name);
$$;

CREATE OR REPLACE FUNCTION __schema__.release_checkpoint_reservation(
  _id bigint
)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
BEGIN
  UPDATE __schema__.checkpoints
  SET status = previous_status,
      previous_status = NULL,
      reserved_until = NULL
  WHERE id = _id
  AND previous_status IS NOT NULL;
END;
$$;

CREATE OR REPLACE FUNCTION __schema__.update_checkpoint_status(
  _id bigint,
  _stream_position bigint,
  _status __schema__.checkpoint_status,
  _last_error jsonb default NULL
)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
BEGIN
  UPDATE __schema__.checkpoints
  SET stream_position = _stream_position,
      reserved_until = NULL,
      status = CASE WHEN _status = 'active' AND stream_version > _stream_position THEN 'lagging' ELSE _status END,
      previous_status = NULL,
      last_error = _last_error
  WHERE id = _id;

  IF (_status = 'retry_pending' OR _status = 'failure_pending') THEN
    PERFORM pg_notify('beckett:retries', NULL);
  END IF;
END;
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

CREATE OR REPLACE FUNCTION __schema__.lock_next_checkpoint_for_retry(
  _group_name text
)
  RETURNS TABLE (
    id bigint,
    group_name text,
    name text,
    stream_name text,
    stream_position bigint,
    status __schema__.checkpoint_status,
    last_error jsonb
  )
  LANGUAGE sql
AS
$$
SELECT c.id,
       c.group_name,
       c.name,
       c.stream_name,
       c.stream_position,
       c.status,
       c.last_error
FROM __schema__.checkpoints c
WHERE c.group_name = _group_name
AND (c.status = 'retry_pending' OR c.status = 'failure_pending')
FOR UPDATE
SKIP LOCKED
LIMIT 1;
$$;

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
WHERE status = 'retrying';
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
