-- types
ALTER TYPE beckett.checkpoint RENAME ATTRIBUTE group_name TO subscription_id;
ALTER TYPE beckett.checkpoint ALTER ATTRIBUTE subscription_id SET DATA TYPE integer;
ALTER TYPE beckett.checkpoint DROP ATTRIBUTE name;

-- tables
CREATE TABLE IF NOT EXISTS beckett.groups
(
  id int PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
  name text NOT NULL UNIQUE,
  global_position bigint NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS beckett.subscriptions_new
(
  id int PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
  group_id int NOT NULL REFERENCES beckett.groups(id),
  name text NOT NULL,
  status beckett.subscription_status DEFAULT 'uninitialized' NOT NULL,
  UNIQUE (group_id, name)
);

CREATE TABLE IF NOT EXISTS beckett.streams
(
  id bigint PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
  name text NOT NULL
);

CREATE TABLE beckett.checkpoints_new (
  id bigint PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
  stream_id bigint NOT NULL REFERENCES beckett.streams (id),
  stream_version bigint NOT NULL DEFAULT 0,
  stream_position bigint NOT NULL DEFAULT 0,
  created_at timestamp with time zone DEFAULT now(),
  updated_at timestamp with time zone DEFAULT now(),
  process_at timestamp with time zone NULL,
  reserved_until timestamp with time zone NULL,
  subscription_id int NOT NULL REFERENCES beckett.subscriptions_new(id),
  lagging boolean GENERATED ALWAYS AS (stream_version > stream_position) STORED,
  status beckett.checkpoint_status NOT NULL DEFAULT 'active',
  retries beckett.retry[] NULL,
  UNIQUE (subscription_id, stream_id)
);

--checkpoint migration procedure
CREATE PROCEDURE beckett.migrate_checkpoints(
    _batch_size integer
)
    LANGUAGE plpgsql
AS
$$
DECLARE
    _checkpoint_id bigint := 0;
    _done boolean := FALSE;
BEGIN
    WHILE NOT _done LOOP
        BEGIN
            WITH checkpoint_batch AS (
                SELECT id
                FROM beckett.checkpoints
                WHERE id > _checkpoint_id
                ORDER BY id
                LIMIT _batch_size
            ),
            migrate_batch AS (
                INSERT INTO beckett.checkpoints_new (stream_id, stream_version, stream_position, created_at, updated_at, process_at, reserved_until, subscription_id, status, retries)
                SELECT st.id,
                       c.stream_version,
                       c.stream_position,
                       c.created_at,
                       c.updated_at,
                       c.process_at,
                       c.reserved_until,
                       s.id,
                       c.status,
                       c.retries
                FROM beckett.checkpoints c
                INNER JOIN checkpoint_batch b ON c.id = b.id
                INNER JOIN beckett.groups g ON c.group_name = g.name
                INNER JOIN beckett.subscriptions_new s on g.id = s.group_id AND c.name = s.name
                INNER JOIN beckett.streams st on c.stream_name = st.name
            )
            SELECT max(id) INTO _checkpoint_id
            FROM checkpoint_batch;

            IF _checkpoint_id IS NULL THEN
                _done := TRUE;
            END IF;

            COMMIT;
        END;
    END LOOP;
END;
$$;

--migrate data
INSERT INTO beckett.groups (name)
SELECT group_name
FROM beckett.subscriptions
GROUP BY group_name;

UPDATE beckett.groups g
SET global_position = d.stream_position
FROM (
  SELECT c.group_name, c.stream_position
  FROM beckett.checkpoints c
  WHERE c.name = '$global'
) d
WHERE g.name = d.group_name;

INSERT INTO beckett.subscriptions_new (group_id, name, status)
SELECT g.id, s.name, s.status
FROM beckett.subscriptions s
INNER JOIN beckett.groups g ON s.group_name = g.name
WHERE s.name != '$global';

INSERT INTO beckett.streams (name)
SELECT stream_name
FROM beckett.checkpoints
GROUP BY stream_name;

CALL beckett.migrate_checkpoints(1000);

-- replace checkpoints table
DROP TABLE beckett.checkpoints;
ALTER TABLE beckett.checkpoints_new RENAME TO checkpoints;
ALTER TABLE beckett.checkpoints RENAME CONSTRAINT checkpoints_new_pkey TO checkpoints_pkey;
ALTER TABLE beckett.checkpoints RENAME CONSTRAINT checkpoints_new_subscription_id_stream_id_key TO checkpoints_subscription_id_stream_id_key;
ALTER TABLE beckett.checkpoints RENAME CONSTRAINT checkpoints_new_stream_id_fkey TO checkpoints_stream_id_fkey;
ALTER TABLE beckett.checkpoints RENAME CONSTRAINT checkpoints_new_subscription_id_fkey TO checkpoints_subscription_id_fkey;

-- replace subscriptions table
DROP TABLE beckett.subscriptions;
ALTER TABLE beckett.subscriptions_new RENAME TO subscriptions;
ALTER TABLE beckett.subscriptions RENAME CONSTRAINT subscriptions_new_pkey TO subscriptions_pkey;
ALTER TABLE beckett.subscriptions RENAME CONSTRAINT subscriptions_new_group_id_name_key TO subscriptions_group_id_name_key;
ALTER TABLE beckett.subscriptions RENAME CONSTRAINT subscriptions_new_group_id_fkey TO subscriptions_group_id_fkey;

-- table permissions
GRANT UPDATE, DELETE ON beckett.groups TO beckett;
GRANT UPDATE, DELETE ON beckett.subscriptions TO beckett;
GRANT UPDATE, DELETE ON beckett.streams TO beckett;
GRANT UPDATE, DELETE ON beckett.checkpoints TO beckett;

-- indexes
CREATE INDEX IF NOT EXISTS ix_subscriptions_status ON beckett.subscriptions (id, status);
CREATE INDEX IF NOT EXISTS ix_subscriptions_active ON beckett.subscriptions (id, group_id, status) WHERE status = 'active';
CREATE UNIQUE INDEX IF NOT EXISTS uix_streams_name ON beckett.streams (name) INCLUDE (id);
CREATE INDEX IF NOT EXISTS ix_checkpoints_to_process ON beckett.checkpoints (subscription_id, process_at, reserved_until)
    WHERE process_at IS NOT NULL AND reserved_until IS NULL;
CREATE INDEX IF NOT EXISTS ix_checkpoints_reserved ON beckett.checkpoints (subscription_id, reserved_until)
    WHERE reserved_until IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_checkpoints_metrics ON beckett.checkpoints (subscription_id, status, lagging);

-- update statistics
ANALYZE beckett.groups;
ANALYZE beckett.streams;
ANALYZE beckett.subscriptions;
ANALYZE beckett.checkpoints;

-- triggers
CREATE OR REPLACE FUNCTION beckett.checkpoint_preprocessor() RETURNS trigger
  LANGUAGE plpgsql
  AS $$
BEGIN
  IF (TG_OP = 'UPDATE') THEN
    NEW.updated_at = now();
  END IF;

  IF (NEW.status = 'active' AND NEW.process_at IS NULL AND NEW.stream_version > NEW.stream_position) THEN
    NEW.process_at = now();
  END IF;

  IF (NEW.process_at IS NOT NULL AND NEW.reserved_until IS NULL) THEN
    PERFORM pg_notify('beckett:checkpoints', NEW.subscription_id::text);
  END IF;

  RETURN NEW;
END;
$$;

CREATE TRIGGER checkpoint_preprocessor BEFORE INSERT OR UPDATE ON beckett.checkpoints
  FOR EACH ROW EXECUTE FUNCTION beckett.checkpoint_preprocessor();

-- drop existing functions
DROP FUNCTION IF EXISTS beckett.add_or_update_subscription(text, text);
DROP FUNCTION IF EXISTS beckett.ensure_checkpoint_exists(text, text, text);
DROP FUNCTION beckett.get_next_uninitialized_subscription(text);
DROP FUNCTION beckett.lock_checkpoint(text, text, text);
DROP FUNCTION beckett.pause_subscription(text, text);
DROP FUNCTION beckett.recover_expired_checkpoint_reservations(text, integer);
DROP FUNCTION beckett.reserve_next_available_checkpoint(text, interval);
DROP FUNCTION beckett.resume_subscription(text, text);
DROP FUNCTION beckett.set_subscription_status(text, text, beckett.subscription_status);
DROP FUNCTION beckett.set_subscription_to_active(_group_name text, _name text);

-- create or replace functions
CREATE FUNCTION beckett.get_next_uninitialized_subscription(_group_id integer) RETURNS TABLE(id integer)
  LANGUAGE sql
AS
$$
SELECT id
FROM beckett.subscriptions
WHERE group_id = _group_id
AND status = 'uninitialized'
LIMIT 1;
$$;

CREATE FUNCTION beckett.get_or_add_group(_name text) RETURNS TABLE(id integer)
  LANGUAGE sql
AS
$$
WITH existing_group_id AS (
  SELECT id
  FROM beckett.groups
  where name = _name
),
new_group_id AS (
  INSERT INTO beckett.groups (name)
  SELECT _name
  WHERE NOT EXISTS (SELECT id FROM existing_group_id)
  ON CONFLICT (name) DO NOTHING
  RETURNING id
)
SELECT id
FROM existing_group_id
UNION ALL
SELECT id
FROM new_group_id
LIMIT 1;
$$;

CREATE FUNCTION beckett.get_or_add_subscription(_group_id integer, _name text) RETURNS TABLE(id integer, status beckett.subscription_status)
  LANGUAGE sql
AS
$$
WITH existing_subscription_id AS (
  SELECT id
  FROM beckett.subscriptions
  WHERE group_id = _group_id
  AND name = _name
)
INSERT INTO beckett.subscriptions (group_id, name)
SELECT _group_id, _name
WHERE NOT EXISTS (SELECT id FROM existing_subscription_id)
ON CONFLICT (group_id, name) DO NOTHING;

SELECT id, status
FROM beckett.subscriptions
WHERE group_id = _group_id
AND name = _name;
$$;

CREATE OR REPLACE FUNCTION beckett.get_subscription_failed_count() RETURNS bigint
  LANGUAGE sql
AS
$$
SELECT count(*)
FROM beckett.subscriptions s
INNER JOIN beckett.checkpoints c ON s.id = c.subscription_id
WHERE s.status != 'uninitialized'
AND c.status = 'failed';;
$$;

CREATE OR REPLACE FUNCTION beckett.get_subscription_lag_count() RETURNS bigint
  LANGUAGE sql
AS
$$
WITH metric AS (
  SELECT
  FROM beckett.subscriptions s
  INNER JOIN beckett.checkpoints c ON s.id = c.subscription_id
  WHERE s.status = 'active'
  AND c.status = 'active'
  AND c.lagging = true
  GROUP BY c.subscription_id
  )
SELECT count(*)
FROM metric;
$$;

CREATE OR REPLACE FUNCTION beckett.get_subscription_metrics() RETURNS TABLE(lagging bigint, retries bigint, failed bigint)
  LANGUAGE sql
AS
$$
WITH lagging AS (
  WITH lagging_subscriptions AS (
    SELECT
    FROM beckett.subscriptions s
    INNER JOIN beckett.checkpoints c ON s.id = c.subscription_id
    WHERE s.status = 'active'
    AND c.status = 'active'
    AND c.lagging = true
    GROUP BY c.subscription_id
  )
  SELECT count(*) AS lagging
  FROM lagging_subscriptions
),
retries AS (
  SELECT count(*) AS retries
  FROM beckett.subscriptions s
  INNER JOIN beckett.checkpoints c ON s.id = c.subscription_id
  WHERE s.status != 'uninitialized'
  AND c.status = 'retry'
),
failed AS (
  SELECT count(*) AS failed
  FROM beckett.subscriptions s
  INNER JOIN beckett.checkpoints c ON s.id = c.subscription_id
  WHERE s.status != 'uninitialized'
  AND c.status = 'failed'
)
SELECT l.lagging, r.retries, f.failed
FROM lagging AS l, retries AS r, failed AS f;
$$;

CREATE OR REPLACE FUNCTION beckett.get_subscription_retry_count() RETURNS bigint
  LANGUAGE sql
AS
$$
SELECT count(*)
FROM beckett.subscriptions s
INNER JOIN beckett.checkpoints c ON s.id = c.subscription_id
WHERE s.status != 'uninitialized'
AND c.status = 'retry';
$$;

CREATE FUNCTION beckett.lock_checkpoint(_subscription_id integer, _stream_name text) RETURNS TABLE(id bigint, stream_position bigint)
  LANGUAGE sql
AS
$$
SELECT c.id, c.stream_position
FROM beckett.checkpoints c
INNER JOIN beckett.streams s ON c.stream_id = s.id
WHERE c.subscription_id = _subscription_id
AND s.name = _stream_name
FOR UPDATE
SKIP LOCKED;
$$;

CREATE FUNCTION beckett.lock_group(_id integer) RETURNS TABLE(id integer, global_position bigint)
  LANGUAGE sql
AS
$$
SELECT id, global_position
FROM beckett.groups
WHERE id = _id
FOR UPDATE
SKIP LOCKED;
$$;

CREATE FUNCTION beckett.pause_subscription(_id integer) RETURNS void
  LANGUAGE sql
AS
$$
UPDATE beckett.subscriptions
SET status = 'paused'
WHERE id = _id;
$$;

CREATE OR REPLACE FUNCTION beckett.record_checkpoints(_checkpoints beckett.checkpoint[]) RETURNS void
  LANGUAGE sql
AS
$$
WITH checkpoints_to_record AS (
  SELECT c.subscription_id, c.stream_name, c.stream_version
  FROM unnest(_checkpoints) c
),
existing_streams AS (
  SELECT s.id, s.name
  FROM beckett.streams s
  INNER JOIN checkpoints_to_record c ON s.name = c.stream_name
),
new_streams AS (
  INSERT INTO beckett.streams (name)
  SELECT c.stream_name
  FROM checkpoints_to_record c
  WHERE NOT EXISTS (select from existing_streams where name = c.stream_name)
  ON CONFLICT (name) DO NOTHING
  RETURNING id, name
),
streams AS (
  SELECT id, name
  FROM existing_streams
  UNION
  SELECT id, name
  FROM new_streams
)
INSERT INTO beckett.checkpoints (stream_id, stream_version, subscription_id)
SELECT s.id, c.stream_version, c.subscription_id
FROM checkpoints_to_record c
INNER JOIN streams s on c.stream_name = s.name
ON CONFLICT (subscription_id, stream_id) DO UPDATE
  SET stream_version = excluded.stream_version;
$$;

CREATE FUNCTION beckett.recover_expired_checkpoint_reservations(_group_id integer, _batch_size integer) RETURNS void
  LANGUAGE sql
AS
$$
UPDATE beckett.checkpoints c
SET reserved_until = NULL
FROM (
  SELECT c.id
  FROM beckett.checkpoints c
  INNER JOIN beckett.subscriptions s ON c.subscription_id = s.id
  WHERE s.group_id = _group_id
  AND c.reserved_until <= now()
  FOR UPDATE SKIP LOCKED
  LIMIT _batch_size
) d
WHERE c.id = d.id;
$$;

CREATE FUNCTION beckett.reserve_next_available_checkpoint(_group_id integer, _reservation_timeout interval) RETURNS TABLE(id bigint, subscription_id integer, stream_name text, stream_position bigint, stream_version bigint, retry_attempts integer, status beckett.checkpoint_status)
  LANGUAGE sql
AS
$$
UPDATE beckett.checkpoints c
SET reserved_until = now() + _reservation_timeout
FROM (
  SELECT c.id, st.name AS stream_name
  FROM beckett.checkpoints c
  INNER JOIN beckett.subscriptions s ON c.subscription_id = s.id
  INNER JOIN beckett.streams st ON c.stream_id = st.id
  WHERE s.group_id = _group_id
  AND s.status = 'active'
  AND c.process_at <= now()
  AND c.reserved_until IS NULL
  ORDER BY c.process_at
  LIMIT 1
  FOR UPDATE
  SKIP LOCKED
) d
WHERE c.id = d.id
RETURNING
  c.id,
  c.subscription_id,
  d.stream_name,
  c.stream_position,
  c.stream_version,
  coalesce(array_length(c.retries, 1), 0) AS retry_attempts,
  c.status;
$$;

CREATE FUNCTION beckett.resume_subscription(_id integer) RETURNS void
  LANGUAGE sql
AS
$$
UPDATE beckett.subscriptions
SET status = 'active'
WHERE id = _id;

SELECT pg_notify('beckett:checkpoints', _id::text);
$$;

CREATE FUNCTION beckett.set_subscription_status(_id integer, _status beckett.subscription_status) RETURNS void
  LANGUAGE sql
AS
$$
UPDATE beckett.subscriptions
SET status = _status
WHERE id = _id;
$$;

CREATE FUNCTION beckett.set_subscription_to_active(_id integer) RETURNS void
  LANGUAGE sql
AS
$_$
DELETE FROM beckett.checkpoints
WHERE subscription_id = _id
AND stream_id = (SELECT id FROM beckett.streams WHERE name = '$initializing');

UPDATE beckett.subscriptions
SET status = 'active'
WHERE id = _id;
$_$;

CREATE FUNCTION beckett.update_group_global_position(_id bigint, _global_position bigint) RETURNS void
  LANGUAGE plpgsql
AS
$$
BEGIN
  UPDATE beckett.groups
  SET global_position = _global_position
  WHERE id = _id;
END;
$$;
