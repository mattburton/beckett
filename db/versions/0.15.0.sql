-- switch from "deleted" to "archived"
ALTER TABLE beckett.messages RENAME COLUMN deleted TO archived;
ALTER TABLE beckett.messages_deleted RENAME TO messages_archived;
ALTER TABLE beckett.messages RENAME CONSTRAINT messages_id_deleted_key TO messages_id_archived_key;
ALTER TABLE beckett.messages RENAME CONSTRAINT messages_stream_name_stream_position_deleted_key TO messages_stream_name_stream_position_archived_key;
ALTER TABLE beckett.messages_active RENAME CONSTRAINT messages_active_id_deleted_key TO messages_active_id_archived_key;
ALTER TABLE beckett.messages_active RENAME CONSTRAINT messages_active_stream_name_stream_position_deleted_key TO messages_active_stream_name_stream_position_archived_key;
ALTER TABLE beckett.messages_archived RENAME CONSTRAINT messages_deleted_id_deleted_key TO messages_archived_id_archived_key;
ALTER TABLE beckett.messages_archived RENAME CONSTRAINT messages_deleted_pkey TO messages_archived_pkey;
ALTER TABLE beckett.messages_archived RENAME CONSTRAINT messages_deleted_stream_name_stream_position_deleted_key TO messages_archived_stream_name_stream_position_archived_key;

CREATE OR REPLACE FUNCTION beckett.append_to_stream(
  _stream_name text,
  _expected_version bigint,
  _messages beckett.message[]
)
  RETURNS bigint
  LANGUAGE plpgsql
AS
$$
DECLARE
  _current_version bigint;
  _stream_version bigint;
BEGIN
  IF (_expected_version < 0) THEN
    PERFORM pg_advisory_xact_lock(beckett.stream_hash(_stream_name));
  END IF;

  SELECT coalesce(max(m.stream_position), 0)
  INTO _current_version
  FROM beckett.messages m
  WHERE m.stream_name = _stream_name
  AND m.archived = false;

  IF (_expected_version < -2) THEN
    RAISE EXCEPTION 'Invalid value for expected version: %', _expected_version;
  END IF;

  IF (_expected_version = -1 AND _current_version = 0) THEN
    RAISE EXCEPTION 'Attempted to append to a non-existing stream: %', _stream_name;
  END IF;

  IF (_expected_version = 0 AND _current_version > 0) THEN
    RAISE EXCEPTION 'Attempted to start a stream that already exists: %', _stream_name;
  END IF;

  IF (_expected_version > 0 AND _expected_version != _current_version) THEN
    RAISE EXCEPTION 'Stream % version % does not match expected version %',
      _stream_name,
      _current_version,
      _expected_version;
  END IF;

  WITH append_messages AS (
    INSERT INTO beckett.messages (
      id,
      stream_position,
      stream_name,
      type,
      data,
      metadata
    )
    SELECT m.id,
           _current_version + (row_number() over())::bigint,
           _stream_name,
           m.type,
           m.data,
           m.metadata
    FROM unnest(_messages) AS m
    RETURNING stream_position, type
  )
  SELECT max(stream_position) INTO _stream_version
  FROM append_messages;

  PERFORM pg_notify('beckett:messages', NULL);

  RETURN _stream_version;
END;
$$;

CREATE OR REPLACE FUNCTION beckett.read_stream(
  _stream_name text,
  _starting_stream_position bigint DEFAULT NULL,
  _ending_stream_position bigint DEFAULT NULL,
  _starting_global_position bigint DEFAULT NULL,
  _ending_global_position bigint DEFAULT NULL,
  _count integer DEFAULT NULL,
  _read_forwards boolean DEFAULT true
)
  RETURNS TABLE (
    id uuid,
    stream_name text,
    stream_version bigint,
    stream_position bigint,
    global_position bigint,
    type text,
    data jsonb,
    metadata jsonb,
    "timestamp" timestamp with time zone
  )
  LANGUAGE plpgsql
AS
$$
DECLARE
  _stream_version bigint;
BEGIN
  SELECT max(m.stream_position)
  INTO _stream_version
  FROM beckett.messages m
  WHERE m.stream_name = _stream_name
  AND m.archived = false;

  IF (_stream_version IS NULL) THEN
    _stream_version = 0;
  END IF;

  RETURN QUERY
    SELECT m.id,
           m.stream_name,
           _stream_version as stream_version,
           m.stream_position,
           m.global_position,
           m.type,
           m.data,
           m.metadata,
           m.timestamp
    FROM beckett.messages m
    WHERE m.stream_name = _stream_name
    AND (_starting_stream_position IS NULL OR m.stream_position >= _starting_stream_position)
    AND (_ending_stream_position IS NULL OR m.stream_position <= _ending_stream_position)
    AND (_starting_global_position IS NULL OR m.global_position >= _starting_global_position)
    AND (_ending_global_position IS NULL OR m.global_position <= _ending_global_position)
    AND m.archived = false
    ORDER BY CASE WHEN _read_forwards = true THEN m.stream_position END,
             CASE WHEN _read_forwards = false THEN m.stream_position END DESC
    LIMIT _count;
END;
$$;

CREATE OR REPLACE FUNCTION beckett.read_global_stream(
  _starting_global_position bigint,
  _batch_size int
)
  RETURNS TABLE (
    stream_name text,
    stream_position bigint,
    global_position bigint,
    type text
  )
  LANGUAGE plpgsql
AS
$$
DECLARE
  _transaction_id xid8;
BEGIN
  SELECT m.transaction_id
  INTO _transaction_id
  FROM beckett.messages m
  WHERE m.global_position = _starting_global_position
  AND m.archived = false;

  IF (_transaction_id IS NULL) THEN
    _transaction_id = '0'::xid8;
  END IF;

  RETURN QUERY
    SELECT m.stream_name,
           m.stream_position,
           m.global_position,
           m.type
    FROM beckett.messages m
    WHERE (m.transaction_id, m.global_position) > (_transaction_id, _starting_global_position)
    AND m.transaction_id < pg_snapshot_xmin(pg_current_snapshot())
    AND m.archived = false
    ORDER BY m.transaction_id, m.global_position
    LIMIT _batch_size;
END;
$$;

-- $tenant support
DROP INDEX IF EXISTS beckett.ix_messages_active_stream_category;

CREATE INDEX IF NOT EXISTS ix_messages_active_tenant_stream_category on beckett.messages_active ((metadata ->> '$tenant'), beckett.stream_category(stream_name))
  WHERE metadata ->> '$tenant' IS NOT NULL;

-- backfill metadata with default tenant
UPDATE beckett.messages
SET metadata = jsonb_set(metadata, '{$tenant}', to_jsonb('default'::text), true)
WHERE metadata ->> '$tenant' IS NULL;

-- skip notification when $global checkpoint is updated
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

  IF (NEW.name != '$global' AND NEW.process_at IS NOT NULL) THEN
    PERFORM pg_notify('beckett:checkpoints', NEW.group_name);
  END IF;

  RETURN NEW;
END;
$$;

-- drop recurring message support
DROP FUNCTION beckett.add_or_update_recurring_message;
DROP FUNCTION beckett.get_recurring_messages_to_deliver;
DROP FUNCTION beckett.update_recurring_message_next_occurrence;
DROP TABLE beckett.recurring_messages;

-- dashboard tenants materialized view
CREATE MATERIALIZED VIEW beckett.tenants AS
SELECT metadata ->> '$tenant' AS tenant
FROM beckett.messages_active
WHERE metadata ->> '$tenant' IS NOT NULL
GROUP BY tenant;

ALTER MATERIALIZED VIEW beckett.tenants OWNER TO beckett;

CREATE UNIQUE INDEX on beckett.tenants (tenant);

-- utility functions
CREATE OR REPLACE FUNCTION beckett.try_advisory_lock(
  _key text
)
  RETURNS boolean
  LANGUAGE sql
AS
$$
SELECT pg_try_advisory_lock(abs(hashtextextended(_key, 0)));
$$;

CREATE OR REPLACE FUNCTION beckett.advisory_unlock(
  _key text
)
  RETURNS boolean
  LANGUAGE sql
AS
$$
SELECT pg_advisory_unlock(abs(hashtextextended(_key, 0)));
$$;

-- combined subscription metrics query
CREATE OR REPLACE FUNCTION beckett.get_subscription_metrics()
  RETURNS TABLE (
    lagging bigint,
    retries bigint,
    failed bigint
  )
  LANGUAGE sql
AS
$$
WITH lagging AS (
    WITH lagging_subscriptions AS (
        SELECT COUNT(*) AS lagging
        FROM beckett.subscriptions s
        INNER JOIN beckett.checkpoints c ON s.group_name = c.group_name AND s.name = c.name
        WHERE s.status = 'active'
        AND c.status = 'active'
        AND c.lagging = TRUE
        GROUP BY c.group_name, c.name
    )
    SELECT count(*) as lagging FROM lagging_subscriptions
    UNION ALL
    SELECT 0
    LIMIT 1
),
retries AS (
    SELECT count(*) as retries
    FROM beckett.subscriptions s
    INNER JOIN beckett.checkpoints c ON s.group_name = c.group_name AND s.name = c.name
    WHERE s.status != 'uninitialized'
    AND c.status = 'retry'
 ),
failed AS (
    SELECT count(*) as failed
    FROM beckett.subscriptions s
    INNER JOIN beckett.checkpoints c ON s.group_name = c.group_name AND s.name = c.name
    WHERE s.status != 'uninitialized'
    AND c.status = 'failed'
)
SELECT l.lagging, r.retries, f.failed
FROM lagging AS l, retries AS r, failed AS f;
$$;
