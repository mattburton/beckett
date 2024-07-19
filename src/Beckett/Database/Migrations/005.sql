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

CREATE TYPE __schema__.checkpoint_status AS ENUM ('active', 'retry', 'pending_failure', 'failed', 'deleted');

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
  stream_version bigint DEFAULT 0 NOT NULL,
  stream_position bigint DEFAULT 0 NOT NULL,
  group_name text NOT NULL,
  name text NOT NULL,
  stream_name text NOT NULL,
  status __schema__.checkpoint_status DEFAULT 'active' NOT NULL,
  last_error jsonb NULL,
  retry_id uuid NULL,
  lagging boolean GENERATED ALWAYS AS ((status = 'active') AND ((stream_version - stream_position) > 0)) STORED,
  retry boolean GENERATED ALWAYS AS ((status = 'retry' OR status = 'pending_failure') AND (retry_id IS NULL)) STORED,
  PRIMARY KEY (group_name, name, stream_name)
);

CREATE INDEX IF NOT EXISTS ix_checkpoints_lagging ON __schema__.checkpoints (lagging);

CREATE INDEX IF NOT EXISTS ix_checkpoints_retry ON __schema__.checkpoints (retry);

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

CREATE FUNCTION __schema__.lock_next_available_checkpoint(
  _group_name text
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
SELECT c.group_name, c.name, c.stream_name, c.stream_position, c.stream_version, c.status
FROM __schema__.checkpoints c
INNER JOIN __schema__.subscriptions s ON c.group_name = s.group_name AND c.name = s.name
WHERE s.group_name = _group_name
AND s.initialized = true
AND c.lagging = true
FOR UPDATE
SKIP LOCKED
LIMIT 1;
$$;

CREATE OR REPLACE FUNCTION __schema__.record_checkpoints(
  _checkpoints __schema__.checkpoint[]
)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
BEGIN
  INSERT INTO __schema__.checkpoints (stream_version, group_name, name, stream_name)
  SELECT c.stream_version, c.group_name, c.name, c.stream_name
  FROM unnest(_checkpoints) c
  ON CONFLICT (group_name, name, stream_name) DO UPDATE
    SET stream_version = excluded.stream_version;

  PERFORM pg_notify('beckett:checkpoints', NULL);
END;
$$;

CREATE OR REPLACE FUNCTION __schema__.update_checkpoint_status(
  _group_name text,
  _name text,
  _stream_name text,
  _stream_position bigint,
  _status __schema__.checkpoint_status,
  _last_error jsonb DEFAULT NULL,
  _retry_id uuid DEFAULT NULL
)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
BEGIN
  UPDATE __schema__.checkpoints
  SET stream_position = _stream_position,
      status = _status,
      retry_id = _retry_id,
      last_error = _last_error
  WHERE group_name = _group_name
  AND name = _name
  AND stream_name = _stream_name;

  IF (_status = 'retry' OR _status = 'pending_failure') THEN
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
INSERT INTO __schema__.checkpoints (stream_version, stream_position, group_name, name, stream_name)
VALUES (_stream_version, _stream_position, _group_name, _name, _stream_name)
ON CONFLICT (group_name, name, stream_name) DO UPDATE
  SET stream_version = excluded.stream_version,
      stream_position = excluded.stream_position;
$$;

CREATE OR REPLACE FUNCTION __schema__.lock_next_checkpoint_for_retry(
  _group_name text
)
  RETURNS TABLE (
    group_name text,
    name text,
    stream_name text,
    stream_position bigint,
    status __schema__.checkpoint_status,
    last_error jsonb,
    retry_id uuid
  )
  LANGUAGE sql
AS
$$
WITH retry AS (
  SELECT c.group_name,
         c.name,
         c.stream_name
  FROM __schema__.checkpoints c
  WHERE c.group_name = _group_name
  AND c.retry = true
  FOR UPDATE
  SKIP LOCKED
  LIMIT 1
)
UPDATE __schema__.checkpoints AS c
SET retry_id = gen_random_uuid()
FROM retry AS r
WHERE c.group_name = r.group_name
AND c.name = r.name
AND c.stream_name = r.stream_name
RETURNING c.group_name, c.name, c.stream_name, c.stream_position, c.status, c.last_error, c.retry_id;
$$;

CREATE OR REPLACE FUNCTION __schema__.get_subscription_lag()
  RETURNS bigint
  LANGUAGE sql
AS
$$
WITH lagging_subscriptions AS (
  SELECT name, group_name, SUM(stream_version - stream_position) AS total_lag
  FROM __schema__.checkpoints
  WHERE status = 'active'
  AND (stream_version - stream_position) > 0
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
