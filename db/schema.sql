--
-- PostgreSQL database dump
--

-- Dumped from database version 16.2
-- Dumped by pg_dump version 16.2

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: beckett; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA beckett;


--
-- Name: checkpoint; Type: TYPE; Schema: beckett; Owner: -
--

CREATE TYPE beckett.checkpoint AS (
	subscription_id integer,
	stream_name text,
	stream_version bigint
);


--
-- Name: checkpoint_status; Type: TYPE; Schema: beckett; Owner: -
--

CREATE TYPE beckett.checkpoint_status AS ENUM (
    'active',
    'retry',
    'failed'
);


--
-- Name: message; Type: TYPE; Schema: beckett; Owner: -
--

CREATE TYPE beckett.message AS (
	id uuid,
	stream_name text,
	type text,
	data jsonb,
	metadata jsonb,
	expected_version bigint
);


--
-- Name: retry; Type: TYPE; Schema: beckett; Owner: -
--

CREATE TYPE beckett.retry AS (
	attempt integer,
	error jsonb,
	"timestamp" timestamp with time zone
);


--
-- Name: scheduled_message; Type: TYPE; Schema: beckett; Owner: -
--

CREATE TYPE beckett.scheduled_message AS (
	id uuid,
	type text,
	data jsonb,
	metadata jsonb,
	deliver_at timestamp with time zone
);


--
-- Name: subscription_status; Type: TYPE; Schema: beckett; Owner: -
--

CREATE TYPE beckett.subscription_status AS ENUM (
    'uninitialized',
    'active',
    'paused',
    'unknown'
);


--
-- Name: advisory_unlock(text); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.advisory_unlock(_key text) RETURNS boolean
    LANGUAGE sql
    AS $$
SELECT pg_advisory_unlock(abs(hashtextextended(_key, 0)));
$$;


--
-- Name: append_to_stream(text, bigint, beckett.message[]); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.append_to_stream(_stream_name text, _expected_version bigint, _messages beckett.message[]) RETURNS bigint
    LANGUAGE plpgsql
    AS $$
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


--
-- Name: cancel_scheduled_message(uuid); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.cancel_scheduled_message(_id uuid) RETURNS void
    LANGUAGE sql
    AS $$
DELETE FROM beckett.scheduled_messages WHERE id = _id;
$$;


--
-- Name: checkpoint_preprocessor(); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.checkpoint_preprocessor() RETURNS trigger
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


--
-- Name: get_next_uninitialized_subscription(integer); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.get_next_uninitialized_subscription(_group_id integer) RETURNS TABLE(id integer)
    LANGUAGE sql
    AS $$
SELECT id
FROM beckett.subscriptions
WHERE group_id = _group_id
AND status = 'uninitialized'
LIMIT 1;
$$;


--
-- Name: get_or_add_group(text); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.get_or_add_group(_name text) RETURNS TABLE(id integer)
    LANGUAGE sql
    AS $$
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


--
-- Name: get_or_add_subscription(integer, text); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.get_or_add_subscription(_group_id integer, _name text) RETURNS TABLE(id integer, status beckett.subscription_status)
    LANGUAGE sql
    AS $$
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


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: scheduled_messages; Type: TABLE; Schema: beckett; Owner: -
--

CREATE TABLE beckett.scheduled_messages (
    id uuid NOT NULL,
    stream_name text NOT NULL,
    type text NOT NULL,
    data jsonb NOT NULL,
    metadata jsonb NOT NULL,
    deliver_at timestamp with time zone NOT NULL,
    "timestamp" timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: get_scheduled_messages_to_deliver(integer); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.get_scheduled_messages_to_deliver(_batch_size integer) RETURNS SETOF beckett.scheduled_messages
    LANGUAGE sql
    AS $$
DELETE FROM beckett.scheduled_messages
WHERE id IN (
  SELECT id
  FROM beckett.scheduled_messages
  WHERE deliver_at <= CURRENT_TIMESTAMP
  FOR UPDATE
  SKIP LOCKED
  LIMIT _batch_size
)
RETURNING *;
$$;


--
-- Name: get_subscription_failed_count(); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.get_subscription_failed_count() RETURNS bigint
    LANGUAGE sql
    AS $$
SELECT count(*)
FROM beckett.subscriptions s
INNER JOIN beckett.checkpoints c ON s.id = c.subscription_id
WHERE s.status != 'uninitialized'
AND c.status = 'failed';;
$$;


--
-- Name: get_subscription_lag_count(); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.get_subscription_lag_count() RETURNS bigint
    LANGUAGE sql
    AS $$
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


--
-- Name: get_subscription_metrics(); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.get_subscription_metrics() RETURNS TABLE(lagging bigint, retries bigint, failed bigint)
    LANGUAGE sql
    AS $$
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


--
-- Name: get_subscription_retry_count(); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.get_subscription_retry_count() RETURNS bigint
    LANGUAGE sql
    AS $$
SELECT count(*)
FROM beckett.subscriptions s
INNER JOIN beckett.checkpoints c ON s.id = c.subscription_id
WHERE s.status != 'uninitialized'
AND c.status = 'retry';
$$;


--
-- Name: lock_checkpoint(integer, text); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.lock_checkpoint(_subscription_id integer, _stream_name text) RETURNS TABLE(id bigint, stream_position bigint)
    LANGUAGE sql
    AS $$
SELECT c.id, c.stream_position
FROM beckett.checkpoints c
INNER JOIN beckett.streams s ON c.stream_id = s.id
WHERE c.subscription_id = _subscription_id
AND s.name = _stream_name
FOR UPDATE
SKIP LOCKED;
$$;


--
-- Name: lock_group(integer); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.lock_group(_id integer) RETURNS TABLE(id integer, global_position bigint)
    LANGUAGE sql
    AS $$
SELECT id, global_position
FROM beckett.groups
WHERE id = _id
FOR UPDATE
SKIP LOCKED;
$$;


--
-- Name: pause_subscription(integer); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.pause_subscription(_id integer) RETURNS void
    LANGUAGE sql
    AS $$
UPDATE beckett.subscriptions
SET status = 'paused'
WHERE id = _id;
$$;


--
-- Name: read_global_stream(bigint, integer); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.read_global_stream(_starting_global_position bigint, _batch_size integer) RETURNS TABLE(stream_name text, stream_position bigint, global_position bigint, type text)
    LANGUAGE plpgsql
    AS $$
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


--
-- Name: read_stream(text, bigint, bigint, bigint, bigint, integer, boolean); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.read_stream(_stream_name text, _starting_stream_position bigint DEFAULT NULL::bigint, _ending_stream_position bigint DEFAULT NULL::bigint, _starting_global_position bigint DEFAULT NULL::bigint, _ending_global_position bigint DEFAULT NULL::bigint, _count integer DEFAULT NULL::integer, _read_forwards boolean DEFAULT true) RETURNS TABLE(id uuid, stream_name text, stream_version bigint, stream_position bigint, global_position bigint, type text, data jsonb, metadata jsonb, "timestamp" timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
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
           _stream_version AS stream_version,
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


--
-- Name: record_checkpoint_error(bigint, bigint, beckett.checkpoint_status, integer, jsonb, timestamp with time zone); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.record_checkpoint_error(_id bigint, _stream_position bigint, _status beckett.checkpoint_status, _attempt integer, _error jsonb, _process_at timestamp with time zone) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
  UPDATE beckett.checkpoints
  SET stream_position = _stream_position,
      process_at = _process_at,
      reserved_until = NULL,
      status = _status,
      retries = array_append(
        coalesce(retries, array[]::beckett.retry[]),
        row(_attempt, _error, now())::beckett.retry
      )
  WHERE id = _id;
END;
$$;


--
-- Name: record_checkpoints(beckett.checkpoint[]); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.record_checkpoints(_checkpoints beckett.checkpoint[]) RETURNS void
    LANGUAGE sql
    AS $$
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


--
-- Name: recover_expired_checkpoint_reservations(integer, integer); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.recover_expired_checkpoint_reservations(_group_id integer, _batch_size integer) RETURNS void
    LANGUAGE sql
    AS $$
UPDATE beckett.checkpoints c
SET reserved_until = NULL
FROM (
    SELECT c.id
    FROM beckett.checkpoints c
    INNER JOIN beckett.subscriptions s on c.subscription_id = s.id
    WHERE s.group_id = _group_id
    AND c.reserved_until <= now()
    FOR UPDATE SKIP LOCKED
    LIMIT _batch_size
) d
WHERE c.id = d.id;
$$;


--
-- Name: release_checkpoint_reservation(bigint); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.release_checkpoint_reservation(_id bigint) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
  UPDATE beckett.checkpoints
  SET process_at = NULL,
      reserved_until = NULL
  WHERE id = _id;
END;
$$;


--
-- Name: reserve_next_available_checkpoint(integer, interval); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.reserve_next_available_checkpoint(_group_id integer, _reservation_timeout interval) RETURNS TABLE(id bigint, subscription_id integer, stream_name text, stream_position bigint, stream_version bigint, retry_attempts integer, status beckett.checkpoint_status)
    LANGUAGE sql
    AS $$
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


--
-- Name: resume_subscription(integer); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.resume_subscription(_id integer) RETURNS void
    LANGUAGE sql
    AS $$
UPDATE beckett.subscriptions
SET status = 'active'
WHERE id = _id;

SELECT pg_notify('beckett:checkpoints', _id::text);
$$;


--
-- Name: schedule_checkpoints(bigint[], timestamp with time zone); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.schedule_checkpoints(_ids bigint[], _process_at timestamp with time zone) RETURNS void
    LANGUAGE sql
    AS $$
UPDATE beckett.checkpoints
SET process_at = _process_at
WHERE id = ANY(_ids);
$$;


--
-- Name: schedule_message(text, beckett.scheduled_message); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.schedule_message(_stream_name text, _scheduled_message beckett.scheduled_message) RETURNS void
    LANGUAGE sql
    AS $$
INSERT INTO beckett.scheduled_messages (
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


--
-- Name: set_subscription_status(integer, beckett.subscription_status); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.set_subscription_status(_id integer, _status beckett.subscription_status) RETURNS void
    LANGUAGE sql
    AS $$
UPDATE beckett.subscriptions
SET status = _status
WHERE id = _id;
$$;


--
-- Name: set_subscription_to_active(integer); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.set_subscription_to_active(_id integer) RETURNS void
    LANGUAGE sql
    AS $_$
DELETE FROM beckett.checkpoints
WHERE subscription_id = _id
AND stream_id = (SELECT id FROM beckett.streams WHERE name = '$initializing');

UPDATE beckett.subscriptions
SET status = 'active'
WHERE id = _id;
$_$;


--
-- Name: skip_checkpoint_position(bigint); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.skip_checkpoint_position(_id bigint) RETURNS void
    LANGUAGE sql
    AS $$
UPDATE beckett.checkpoints
SET stream_position = CASE WHEN stream_position + 1 > stream_version THEN stream_position ELSE stream_position + 1 END,
    process_at = NULL,
    reserved_until = NULL,
    status = 'active',
    retries = NULL
WHERE id = _id;
$$;


--
-- Name: stream_category(text); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.stream_category(_stream_name text) RETURNS text
    LANGUAGE sql IMMUTABLE
    AS $$
SELECT split_part(_stream_name, '-', 1);
$$;


--
-- Name: stream_hash(text); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.stream_hash(_stream_name text) RETURNS bigint
    LANGUAGE sql IMMUTABLE
    AS $$
SELECT abs(hashtextextended(_stream_name, 0));
$$;


--
-- Name: try_advisory_lock(text); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.try_advisory_lock(_key text) RETURNS boolean
    LANGUAGE sql
    AS $$
SELECT pg_try_advisory_lock(abs(hashtextextended(_key, 0)));
$$;


--
-- Name: update_checkpoint_position(bigint, bigint, timestamp with time zone); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.update_checkpoint_position(_id bigint, _stream_position bigint, _process_at timestamp with time zone) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
  UPDATE beckett.checkpoints
  SET stream_position = _stream_position,
      process_at = _process_at,
      reserved_until = NULL,
      status = 'active',
      retries = NULL
  WHERE id = _id;
END;
$$;


--
-- Name: update_group_global_position(bigint, bigint); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.update_group_global_position(_id bigint, _global_position bigint) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
  UPDATE beckett.groups
  SET global_position = _global_position
  WHERE id = _id;
END;
$$;


--
-- Name: update_system_checkpoint_position(bigint, bigint); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.update_system_checkpoint_position(_id bigint, _position bigint) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
  UPDATE beckett.checkpoints
  SET stream_version = _position,
      stream_position = _position
  WHERE id = _id;
END;
$$;


--
-- Name: checkpoints; Type: TABLE; Schema: beckett; Owner: -
--

CREATE TABLE beckett.checkpoints (
    id bigint NOT NULL,
    stream_id bigint NOT NULL,
    stream_version bigint DEFAULT 0 NOT NULL,
    stream_position bigint DEFAULT 0 NOT NULL,
    created_at timestamp with time zone DEFAULT now(),
    updated_at timestamp with time zone DEFAULT now(),
    process_at timestamp with time zone,
    reserved_until timestamp with time zone,
    subscription_id integer NOT NULL,
    lagging boolean GENERATED ALWAYS AS ((stream_version > stream_position)) STORED,
    status beckett.checkpoint_status DEFAULT 'active'::beckett.checkpoint_status NOT NULL,
    retries beckett.retry[]
);


--
-- Name: checkpoints_id_seq; Type: SEQUENCE; Schema: beckett; Owner: -
--

ALTER TABLE beckett.checkpoints ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME beckett.checkpoints_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: groups; Type: TABLE; Schema: beckett; Owner: -
--

CREATE TABLE beckett.groups (
    id integer NOT NULL,
    name text NOT NULL,
    global_position bigint DEFAULT 0 NOT NULL
);


--
-- Name: groups_id_seq; Type: SEQUENCE; Schema: beckett; Owner: -
--

ALTER TABLE beckett.groups ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME beckett.groups_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: messages; Type: TABLE; Schema: beckett; Owner: -
--

CREATE TABLE beckett.messages (
    id uuid NOT NULL,
    global_position bigint NOT NULL,
    stream_position bigint NOT NULL,
    transaction_id xid8 DEFAULT pg_current_xact_id() NOT NULL,
    "timestamp" timestamp with time zone DEFAULT now() NOT NULL,
    archived boolean DEFAULT false NOT NULL,
    stream_name text NOT NULL,
    type text NOT NULL,
    data jsonb NOT NULL,
    metadata jsonb NOT NULL
)
PARTITION BY LIST (archived);


--
-- Name: messages_active; Type: TABLE; Schema: beckett; Owner: -
--

CREATE TABLE beckett.messages_active (
    id uuid NOT NULL,
    global_position bigint NOT NULL,
    stream_position bigint NOT NULL,
    transaction_id xid8 DEFAULT pg_current_xact_id() NOT NULL,
    "timestamp" timestamp with time zone DEFAULT now() NOT NULL,
    archived boolean DEFAULT false NOT NULL,
    stream_name text NOT NULL,
    type text NOT NULL,
    data jsonb NOT NULL,
    metadata jsonb NOT NULL
);


--
-- Name: messages_archived; Type: TABLE; Schema: beckett; Owner: -
--

CREATE TABLE beckett.messages_archived (
    id uuid NOT NULL,
    global_position bigint NOT NULL,
    stream_position bigint NOT NULL,
    transaction_id xid8 DEFAULT pg_current_xact_id() NOT NULL,
    "timestamp" timestamp with time zone DEFAULT now() NOT NULL,
    archived boolean DEFAULT false NOT NULL,
    stream_name text NOT NULL,
    type text NOT NULL,
    data jsonb NOT NULL,
    metadata jsonb NOT NULL
);


--
-- Name: messages_global_position_seq; Type: SEQUENCE; Schema: beckett; Owner: -
--

ALTER TABLE beckett.messages ALTER COLUMN global_position ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME beckett.messages_global_position_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: migrations; Type: TABLE; Schema: beckett; Owner: -
--

CREATE TABLE beckett.migrations (
    name text NOT NULL,
    "timestamp" timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: streams; Type: TABLE; Schema: beckett; Owner: -
--

CREATE TABLE beckett.streams (
    id bigint NOT NULL,
    name text NOT NULL
);


--
-- Name: streams_id_seq; Type: SEQUENCE; Schema: beckett; Owner: -
--

ALTER TABLE beckett.streams ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME beckett.streams_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: subscriptions; Type: TABLE; Schema: beckett; Owner: -
--

CREATE TABLE beckett.subscriptions (
    id integer NOT NULL,
    group_id integer NOT NULL,
    name text NOT NULL,
    status beckett.subscription_status DEFAULT 'uninitialized'::beckett.subscription_status NOT NULL
);


--
-- Name: subscriptions_id_seq; Type: SEQUENCE; Schema: beckett; Owner: -
--

ALTER TABLE beckett.subscriptions ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME beckett.subscriptions_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- Name: tenants; Type: MATERIALIZED VIEW; Schema: beckett; Owner: -
--

CREATE MATERIALIZED VIEW beckett.tenants AS
 SELECT (metadata ->> '$tenant'::text) AS tenant
   FROM beckett.messages_active
  WHERE ((metadata ->> '$tenant'::text) IS NOT NULL)
  GROUP BY (metadata ->> '$tenant'::text)
  WITH NO DATA;


--
-- Name: messages_active; Type: TABLE ATTACH; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.messages ATTACH PARTITION beckett.messages_active FOR VALUES IN (false);


--
-- Name: messages_archived; Type: TABLE ATTACH; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.messages ATTACH PARTITION beckett.messages_archived FOR VALUES IN (true);


--
-- Name: checkpoints checkpoints_pkey; Type: CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.checkpoints
    ADD CONSTRAINT checkpoints_pkey PRIMARY KEY (id);


--
-- Name: checkpoints checkpoints_subscription_id_stream_id_key; Type: CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.checkpoints
    ADD CONSTRAINT checkpoints_subscription_id_stream_id_key UNIQUE (subscription_id, stream_id);


--
-- Name: groups groups_name_key; Type: CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.groups
    ADD CONSTRAINT groups_name_key UNIQUE (name);


--
-- Name: groups groups_pkey; Type: CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.groups
    ADD CONSTRAINT groups_pkey PRIMARY KEY (id);


--
-- Name: messages messages_id_archived_key; Type: CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.messages
    ADD CONSTRAINT messages_id_archived_key UNIQUE (id, archived);


--
-- Name: messages_active messages_active_id_archived_key; Type: CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.messages_active
    ADD CONSTRAINT messages_active_id_archived_key UNIQUE (id, archived);


--
-- Name: messages messages_pkey; Type: CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.messages
    ADD CONSTRAINT messages_pkey PRIMARY KEY (global_position, archived);


--
-- Name: messages_active messages_active_pkey; Type: CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.messages_active
    ADD CONSTRAINT messages_active_pkey PRIMARY KEY (global_position, archived);


--
-- Name: messages messages_stream_name_stream_position_archived_key; Type: CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.messages
    ADD CONSTRAINT messages_stream_name_stream_position_archived_key UNIQUE (stream_name, stream_position, archived);


--
-- Name: messages_active messages_active_stream_name_stream_position_archived_key; Type: CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.messages_active
    ADD CONSTRAINT messages_active_stream_name_stream_position_archived_key UNIQUE (stream_name, stream_position, archived);


--
-- Name: messages_archived messages_archived_id_archived_key; Type: CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.messages_archived
    ADD CONSTRAINT messages_archived_id_archived_key UNIQUE (id, archived);


--
-- Name: messages_archived messages_archived_pkey; Type: CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.messages_archived
    ADD CONSTRAINT messages_archived_pkey PRIMARY KEY (global_position, archived);


--
-- Name: messages_archived messages_archived_stream_name_stream_position_archived_key; Type: CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.messages_archived
    ADD CONSTRAINT messages_archived_stream_name_stream_position_archived_key UNIQUE (stream_name, stream_position, archived);


--
-- Name: migrations migrations_pkey; Type: CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.migrations
    ADD CONSTRAINT migrations_pkey PRIMARY KEY (name);


--
-- Name: scheduled_messages scheduled_messages_pkey; Type: CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.scheduled_messages
    ADD CONSTRAINT scheduled_messages_pkey PRIMARY KEY (id);


--
-- Name: streams streams_pkey; Type: CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.streams
    ADD CONSTRAINT streams_pkey PRIMARY KEY (id);


--
-- Name: subscriptions subscriptions_group_id_name_key; Type: CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.subscriptions
    ADD CONSTRAINT subscriptions_group_id_name_key UNIQUE (group_id, name);


--
-- Name: subscriptions subscriptions_pkey; Type: CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.subscriptions
    ADD CONSTRAINT subscriptions_pkey PRIMARY KEY (id);


--
-- Name: ix_checkpoints_metrics; Type: INDEX; Schema: beckett; Owner: -
--

CREATE INDEX ix_checkpoints_metrics ON beckett.checkpoints USING btree (subscription_id, status, lagging);


--
-- Name: ix_checkpoints_reserved; Type: INDEX; Schema: beckett; Owner: -
--

CREATE INDEX ix_checkpoints_reserved ON beckett.checkpoints USING btree (subscription_id, reserved_until) WHERE (reserved_until IS NOT NULL);


--
-- Name: ix_checkpoints_to_process; Type: INDEX; Schema: beckett; Owner: -
--

CREATE INDEX ix_checkpoints_to_process ON beckett.checkpoints USING btree (subscription_id, process_at, reserved_until) WHERE ((process_at IS NOT NULL) AND (reserved_until IS NULL));


--
-- Name: ix_messages_active_correlation_id; Type: INDEX; Schema: beckett; Owner: -
--

CREATE INDEX ix_messages_active_correlation_id ON beckett.messages_active USING btree (((metadata ->> '$correlation_id'::text))) WHERE ((metadata ->> '$correlation_id'::text) IS NOT NULL);


--
-- Name: ix_messages_active_global_read_stream; Type: INDEX; Schema: beckett; Owner: -
--

CREATE INDEX ix_messages_active_global_read_stream ON beckett.messages_active USING btree (transaction_id, global_position, archived);


--
-- Name: ix_messages_active_tenant_stream_category; Type: INDEX; Schema: beckett; Owner: -
--

CREATE INDEX ix_messages_active_tenant_stream_category ON beckett.messages_active USING btree (((metadata ->> '$tenant'::text)), beckett.stream_category(stream_name)) WHERE ((metadata ->> '$tenant'::text) IS NOT NULL);


--
-- Name: ix_scheduled_messages_deliver_at; Type: INDEX; Schema: beckett; Owner: -
--

CREATE INDEX ix_scheduled_messages_deliver_at ON beckett.scheduled_messages USING btree (deliver_at);


--
-- Name: ix_subscriptions_active; Type: INDEX; Schema: beckett; Owner: -
--

CREATE INDEX ix_subscriptions_active ON beckett.subscriptions USING btree (id, group_id, status) WHERE (status = 'active'::beckett.subscription_status);


--
-- Name: ix_subscriptions_status; Type: INDEX; Schema: beckett; Owner: -
--

CREATE INDEX ix_subscriptions_status ON beckett.subscriptions USING btree (id, status);


--
-- Name: tenants_tenant_idx; Type: INDEX; Schema: beckett; Owner: -
--

CREATE UNIQUE INDEX tenants_tenant_idx ON beckett.tenants USING btree (tenant);


--
-- Name: uix_streams_name; Type: INDEX; Schema: beckett; Owner: -
--

CREATE UNIQUE INDEX uix_streams_name ON beckett.streams USING btree (name) INCLUDE (id);


--
-- Name: messages_active_id_archived_key; Type: INDEX ATTACH; Schema: beckett; Owner: -
--

ALTER INDEX beckett.messages_id_archived_key ATTACH PARTITION beckett.messages_active_id_archived_key;


--
-- Name: messages_active_pkey; Type: INDEX ATTACH; Schema: beckett; Owner: -
--

ALTER INDEX beckett.messages_pkey ATTACH PARTITION beckett.messages_active_pkey;


--
-- Name: messages_active_stream_name_stream_position_archived_key; Type: INDEX ATTACH; Schema: beckett; Owner: -
--

ALTER INDEX beckett.messages_stream_name_stream_position_archived_key ATTACH PARTITION beckett.messages_active_stream_name_stream_position_archived_key;


--
-- Name: messages_archived_id_archived_key; Type: INDEX ATTACH; Schema: beckett; Owner: -
--

ALTER INDEX beckett.messages_id_archived_key ATTACH PARTITION beckett.messages_archived_id_archived_key;


--
-- Name: messages_archived_pkey; Type: INDEX ATTACH; Schema: beckett; Owner: -
--

ALTER INDEX beckett.messages_pkey ATTACH PARTITION beckett.messages_archived_pkey;


--
-- Name: messages_archived_stream_name_stream_position_archived_key; Type: INDEX ATTACH; Schema: beckett; Owner: -
--

ALTER INDEX beckett.messages_stream_name_stream_position_archived_key ATTACH PARTITION beckett.messages_archived_stream_name_stream_position_archived_key;


--
-- Name: checkpoints checkpoint_preprocessor; Type: TRIGGER; Schema: beckett; Owner: -
--

CREATE TRIGGER checkpoint_preprocessor BEFORE INSERT OR UPDATE ON beckett.checkpoints FOR EACH ROW EXECUTE FUNCTION beckett.checkpoint_preprocessor();


--
-- Name: checkpoints checkpoints_stream_id_fkey; Type: FK CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.checkpoints
    ADD CONSTRAINT checkpoints_stream_id_fkey FOREIGN KEY (stream_id) REFERENCES beckett.streams(id);


--
-- Name: checkpoints checkpoints_subscription_id_fkey; Type: FK CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.checkpoints
    ADD CONSTRAINT checkpoints_subscription_id_fkey FOREIGN KEY (subscription_id) REFERENCES beckett.subscriptions(id);


--
-- Name: subscriptions subscriptions_group_id_fkey; Type: FK CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.subscriptions
    ADD CONSTRAINT subscriptions_group_id_fkey FOREIGN KEY (group_id) REFERENCES beckett.groups(id);


--
-- PostgreSQL database dump complete
--

