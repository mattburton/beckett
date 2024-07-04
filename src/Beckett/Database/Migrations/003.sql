-------------------------------------------------
-- SCHEDULED MESSAGE SUPPORT
-------------------------------------------------

CREATE TYPE __schema__.scheduled_message AS
(
  application text,
  id uuid,
  type text,
  data jsonb,
  metadata jsonb,
  deliver_at timestamp with time zone
);

CREATE TABLE __schema__.scheduled_messages
(
  application text NOT NULL,
  id uuid NOT NULL,
  stream_name text NOT NULL,
  type text NOT NULL,
  data jsonb NOT NULL,
  metadata jsonb NOT NULL,
  deliver_at timestamp with time zone NOT NULL,
  timestamp timestamp with time zone DEFAULT now() NOT NULL,
  PRIMARY KEY (application, id)
);

GRANT UPDATE, DELETE ON __schema__.scheduled_messages TO beckett;

CREATE OR REPLACE FUNCTION __schema__.schedule_message(
  _application text,
  _stream_name text,
  _scheduled_message __schema__.scheduled_message
)
  RETURNS void
  LANGUAGE sql
AS
$$
INSERT INTO __schema__.scheduled_messages (
  application,
  id,
  stream_name,
  type,
  data,
  metadata,
  deliver_at
)
VALUES (
  _application,
  _scheduled_message.id,
  _stream_name,
  _scheduled_message.type,
  _scheduled_message.data,
  _scheduled_message.metadata,
  _scheduled_message.deliver_at
)
ON CONFLICT (application, id) DO NOTHING;
$$;

CREATE OR REPLACE FUNCTION __schema__.get_scheduled_messages_to_deliver(
  _application text,
  _batch_size int
)
  RETURNS setof __schema__.scheduled_messages
  LANGUAGE sql
AS
$$
DELETE FROM __schema__.scheduled_messages
WHERE id IN (
  SELECT id
  FROM __schema__.scheduled_messages
  WHERE application = _application
  AND deliver_at <= CURRENT_TIMESTAMP
  FOR UPDATE
  SKIP LOCKED
  LIMIT _batch_size
)
RETURNING *;
$$;

CREATE OR REPLACE FUNCTION __schema__.cancel_scheduled_message(
  _application text,
  _id uuid
)
  RETURNS void
  LANGUAGE sql
AS
$$
DELETE FROM __schema__.scheduled_messages WHERE application = _application AND id = _id;
$$;
