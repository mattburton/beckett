-- $tenant support
DROP INDEX IF EXISTS beckett.ix_messages_active_stream_category;

CREATE INDEX IF NOT EXISTS ix_messages_active_tenant_stream_category on beckett.messages_active ((metadata ->> '$tenant'), beckett.stream_category(stream_name))
  WHERE metadata ->> '$tenant' IS NOT NULL;

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