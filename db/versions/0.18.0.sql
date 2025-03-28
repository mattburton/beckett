-- record categories, tenants
DROP MATERIALIZED VIEW beckett.tenants;

CREATE TABLE IF NOT EXISTS beckett.categories
(
  name text NOT NULL PRIMARY KEY,
  updated_at timestamp with time zone DEFAULT now() NOT NULL
);

GRANT UPDATE, DELETE ON beckett.categories TO beckett;

CREATE TABLE IF NOT EXISTS beckett.tenants
(
  tenant text NOT NULL PRIMARY KEY
);

GRANT UPDATE, DELETE ON beckett.tenants TO beckett;

CREATE OR REPLACE FUNCTION beckett.record_stream_data(
  _category_names text[],
  _category_timestamps timestamp with time zone[],
  _tenants text[]
)
  RETURNS void
  LANGUAGE sql
AS
$$
INSERT INTO beckett.categories (name, updated_at)
SELECT d.name, d.timestamp
FROM unnest(_category_names, _category_timestamps) AS d (name, timestamp)
ON CONFLICT (name) DO UPDATE
  SET updated_at = excluded.updated_at;

INSERT INTO beckett.tenants (tenant)
SELECT d.tenant
FROM unnest(_tenants) AS d (tenant)
ON CONFLICT (tenant) DO NOTHING;
$$;

-- populate existing categories and tenants
INSERT INTO beckett.categories (name, updated_at)
SELECT beckett.stream_category(stream_name), max(timestamp)
FROM beckett.messages_active
GROUP BY beckett.stream_category(stream_name);

INSERT INTO beckett.tenants (tenant)
SELECT metadata ->> '$tenant' AS name
FROM beckett.messages_active
WHERE metadata ->> '$tenant' IS NOT NULL
GROUP BY name;
