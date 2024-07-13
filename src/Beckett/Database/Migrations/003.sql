-------------------------------------------------
-- SCHEDULED MESSAGE SUPPORT
-------------------------------------------------

CREATE TYPE __schema__.scheduled_message AS
(
  id uuid,
  type text,
  data jsonb,
  metadata jsonb,
  deliver_at timestamp with time zone
);

CREATE TABLE __schema__.scheduled_messages
(
  id uuid NOT NULL PRIMARY KEY,
  stream_name text NOT NULL,
  type text NOT NULL,
  data jsonb NOT NULL,
  metadata jsonb NOT NULL,
  deliver_at timestamp with time zone NOT NULL,
  timestamp timestamp with time zone DEFAULT now() NOT NULL
);

CREATE INDEX ix_scheduled_messages_deliver_at ON __schema__.scheduled_messages (deliver_at ASC);

GRANT UPDATE, DELETE ON __schema__.scheduled_messages TO beckett;

CREATE OR REPLACE FUNCTION __schema__.schedule_message(
  _stream_name text,
  _scheduled_message __schema__.scheduled_message
)
  RETURNS void
  LANGUAGE sql
AS
$$
INSERT INTO __schema__.scheduled_messages (
  id,
  stream_name,
  type,
  data,
  metadata,
  deliver_at
)
VALUES (
  _scheduled_message.id,
  _stream_name,
  _scheduled_message.type,
  _scheduled_message.data,
  _scheduled_message.metadata,
  _scheduled_message.deliver_at
)
ON CONFLICT (id) DO NOTHING;
$$;

CREATE OR REPLACE FUNCTION __schema__.get_scheduled_messages_to_deliver(
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
  WHERE deliver_at <= CURRENT_TIMESTAMP
  FOR UPDATE
  SKIP LOCKED
  LIMIT _batch_size
)
RETURNING *;
$$;

CREATE OR REPLACE FUNCTION __schema__.cancel_scheduled_message(
  _id uuid
)
  RETURNS void
  LANGUAGE sql
AS
$$
DELETE FROM __schema__.scheduled_messages WHERE id = _id;
$$;
