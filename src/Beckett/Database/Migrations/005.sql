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

CREATE TYPE __schema__.retry AS
(
  attempt int,
  error jsonb,
  timestamp timestamp with time zone
);

CREATE TYPE __schema__.subscription_status AS ENUM (
  'uninitialized',
  'active'
);

CREATE TYPE __schema__.checkpoint_status AS ENUM (
  'active',
  'retry',
  'failed'
);

CREATE TABLE IF NOT EXISTS __schema__.subscriptions
(
  group_name text NOT NULL,
  name text NOT NULL,
  status __schema__.subscription_status DEFAULT 'uninitialized' NOT NULL,
  PRIMARY KEY (group_name, name)
);

CREATE INDEX ix_subscriptions_active ON beckett.subscriptions (group_name, name, status) WHERE status = 'active';

GRANT UPDATE, DELETE ON __schema__.subscriptions TO beckett;

CREATE TABLE IF NOT EXISTS __schema__.checkpoints
(
  id bigint PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
  stream_version bigint NOT NULL DEFAULT 0,
  stream_position bigint NOT NULL DEFAULT 0,
  created_at timestamp with time zone DEFAULT now(),
  updated_at timestamp with time zone DEFAULT now(),
  process_at timestamp with time zone NULL,
  reserved_until timestamp with time zone NULL,
  lagging boolean GENERATED ALWAYS AS (stream_version > stream_position) STORED,
  status __schema__.checkpoint_status NOT NULL DEFAULT 'active',
  group_name text NOT NULL,
  name text NOT NULL,
  stream_name text NOT NULL,
  retries __schema__.retry[] NULL,
  UNIQUE (group_name, name, stream_name)
);

CREATE INDEX IF NOT EXISTS ix_checkpoints_to_process ON beckett.checkpoints (group_name, process_at, reserved_until)
  WHERE process_at IS NOT NULL AND reserved_until IS NULL;

CREATE INDEX IF NOT EXISTS ix_checkpoints_reserved ON __schema__.checkpoints (group_name, reserved_until)
  WHERE reserved_until IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_checkpoints_metrics ON beckett.checkpoints (status, lagging, group_name, name);

CREATE FUNCTION __schema__.checkpoint_preprocessor()
  RETURNS trigger
  LANGUAGE plpgsql
AS
$$
BEGIN
  IF (TG_OP = 'UPDATE') THEN
    NEW.updated_at = now();
  END IF;

  IF (NEW.status = 'active' AND NEW.process_at IS NULL AND NEW.stream_version > NEW.stream_position) THEN
    NEW.process_at = now();
  END IF;

  IF (NEW.process_at IS NOT NULL) THEN
    PERFORM pg_notify('beckett:checkpoints', NEW.group_name);
  END IF;

  RETURN NEW;
END;
$$;

CREATE TRIGGER checkpoint_preprocessor BEFORE INSERT OR UPDATE ON __schema__.checkpoints
  FOR EACH ROW EXECUTE FUNCTION __schema__.checkpoint_preprocessor();

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
    id bigint,
    stream_position bigint
  )
  LANGUAGE sql
AS
$$
SELECT id, stream_position
FROM __schema__.checkpoints
WHERE group_name = _group_name
AND name = _name
AND stream_name = _stream_name
FOR UPDATE
SKIP LOCKED;
$$;

CREATE OR REPLACE FUNCTION __schema__.record_checkpoint_error(
  _id bigint,
  _stream_position bigint,
  _status __schema__.checkpoint_status,
  _attempt int,
  _error jsonb,
  _process_at timestamp with time zone
)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
BEGIN
  UPDATE __schema__.checkpoints
  SET stream_position = _stream_position,
      process_at = _process_at,
      reserved_until = NULL,
      status = _status,
      retries = array_append(
        coalesce(retries, array[]::__schema__.retry[]),
        row(_attempt, _error, now())::__schema__.retry
      )
  WHERE id = _id;
END;
$$;

CREATE OR REPLACE FUNCTION __schema__.record_checkpoints(
  _checkpoints __schema__.checkpoint[]
)
  RETURNS void
  LANGUAGE sql
AS
$$
INSERT INTO __schema__.checkpoints (stream_version, group_name, name, stream_name)
SELECT c.stream_version, c.group_name, c.name, c.stream_name
FROM unnest(_checkpoints) c
ON CONFLICT (group_name, name, stream_name) DO UPDATE
  SET stream_version = excluded.stream_version;
$$;

CREATE OR REPLACE FUNCTION __schema__.recover_expired_checkpoint_reservations(
  _group_name text,
  _batch_size int
)
  RETURNS void
  LANGUAGE sql
AS
$$
UPDATE __schema__.checkpoints c
SET reserved_until = NULL
FROM (
    SELECT id
    FROM __schema__.checkpoints
    WHERE group_name = _group_name
    AND reserved_until <= now()
    FOR UPDATE SKIP LOCKED
    LIMIT _batch_size
) as d
WHERE c.id = d.id;
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
    retry_attempts int,
    status __schema__.checkpoint_status
  )
  LANGUAGE sql
AS
$$
UPDATE __schema__.checkpoints c
SET reserved_until = now() + _reservation_timeout
FROM (
  SELECT c.id
  FROM __schema__.checkpoints c
  INNER JOIN __schema__.subscriptions s ON c.group_name = s.group_name AND c.name = s.name
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

CREATE OR REPLACE FUNCTION __schema__.schedule_checkpoints(
  _ids bigint[],
  _process_at timestamp with time zone
)
  RETURNS void
  LANGUAGE sql
AS
$$
UPDATE __schema__.checkpoints
SET process_at = _process_at
WHERE id = ANY(_ids);
$$;

CREATE OR REPLACE FUNCTION __schema__.update_system_checkpoint_position(
  _id bigint,
  _position bigint
)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
BEGIN
  UPDATE __schema__.checkpoints
  SET stream_version = _position,
      stream_position = _position
  WHERE id = _id;
END;
$$;

CREATE OR REPLACE FUNCTION __schema__.update_checkpoint_position(
  _id bigint,
  _stream_position bigint,
  _process_at timestamp with time zone
)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
BEGIN
  UPDATE __schema__.checkpoints
  SET stream_position = _stream_position,
      process_at = _process_at,
      reserved_until = NULL,
      status = 'active',
      retries = NULL
  WHERE id = _id;
END;
$$;

-------------------------------------------------
-- METRICS
-------------------------------------------------
CREATE OR REPLACE FUNCTION __schema__.get_subscription_lag_count()
  RETURNS bigint
  LANGUAGE sql
AS
$$
WITH metric AS (
    SELECT
    FROM __schema__.subscriptions s
    INNER JOIN __schema__.checkpoints c ON s.group_name = c.group_name AND s.name = c.name
    WHERE s.status = 'active'
    AND c.status = 'active'
    AND c.lagging = true
    GROUP BY c.group_name, c.name
)
SELECT count(*)
FROM metric;
$$;

CREATE OR REPLACE FUNCTION __schema__.get_subscription_retry_count()
  RETURNS bigint
  LANGUAGE sql
AS
$$
WITH metric AS (
    SELECT count(*) as value
    FROM __schema__.subscriptions s
    INNER JOIN __schema__.checkpoints c ON s.group_name = c.group_name AND s.name = c.name
    WHERE s.status = 'active'
    AND c.status = 'retry'
    UNION ALL
    SELECT 0
)
SELECT value
FROM metric
LIMIT 1;
$$;

CREATE OR REPLACE FUNCTION __schema__.get_subscription_failed_count()
  RETURNS bigint
  LANGUAGE sql
AS
$$
WITH metric AS (
    SELECT count(*) as value
    FROM __schema__.subscriptions s
    INNER JOIN __schema__.checkpoints c ON s.group_name = c.group_name AND s.name = c.name
    WHERE s.status = 'active'
    AND c.status = 'failed'
    UNION ALL
    SELECT 0
)
SELECT value
FROM metric
LIMIT 1;
$$;
