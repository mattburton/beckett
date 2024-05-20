-------------------------------------------------
-- RECURRING MESSAGE SUPPORT
-------------------------------------------------

CREATE TABLE __schema__.recurring_messages
(
  application text NOT NULL,
  name text NOT NULL,
  cron_expression text NOT NULL,
  stream_name text NOT NULL,
  type text NOT NULL,
  data jsonb NOT NULL,
  metadata jsonb NOT NULL,
  next_occurrence timestamp with time zone NULL,
  complete boolean DEFAULT false NOT NULL,
  timestamp timestamp with time zone DEFAULT now() NOT NULL,
  PRIMARY KEY (application, name)
);

GRANT UPDATE, DELETE ON __schema__.recurring_messages TO beckett;

CREATE OR REPLACE FUNCTION __schema__.add_or_update_recurring_message(
  _application text,
  _name text,
  _cron_expression text,
  _stream_name text,
  _type text,
  _data jsonb,
  _metadata jsonb,
  _next_occurrence timestamp with time zone
)
  RETURNS void
  LANGUAGE sql
AS
$$
INSERT INTO __schema__.recurring_messages (
  application,
  name,
  cron_expression,
  stream_name,
  type,
  data,
  metadata,
  next_occurrence
)
VALUES (_application, _name, _cron_expression, _stream_name, _type, _data, _metadata, _next_occurrence)
ON CONFLICT (application, name) DO UPDATE
  SET cron_expression = excluded.cron_expression,
      stream_name = excluded.stream_name,
      data = excluded.data,
      metadata = excluded.metadata,
      next_occurrence = excluded.next_occurrence;
$$;

CREATE OR REPLACE FUNCTION __schema__.get_recurring_messages_to_deliver(
  _application text,
  _batch_size int
)
  RETURNS setof __schema__.recurring_messages
  LANGUAGE sql
AS
$$
SELECT application, name, cron_expression, stream_name, type, data, metadata, next_occurrence, complete, timestamp
FROM __schema__.recurring_messages
WHERE application = _application
AND complete = false
AND next_occurrence <= CURRENT_TIMESTAMP
FOR UPDATE
SKIP LOCKED
LIMIT _batch_size;
$$;

CREATE OR REPLACE FUNCTION __schema__.update_recurring_message_next_occurrence(
  _application text,
  _name text,
  _next_occurrence timestamp with time zone,
  _complete boolean
)
  RETURNS void
  LANGUAGE sql
AS
$$
UPDATE __schema__.recurring_messages
SET next_occurrence = _next_occurrence,
    complete = _complete
WHERE application = _application
AND name = _name;
$$;
