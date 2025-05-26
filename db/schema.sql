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
	group_name text,
	name text,
	stream_name text,
	stream_version bigint,
	stream_position bigint
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
-- Name: add_or_update_subscription(text, text); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.add_or_update_subscription(_group_name text, _name text) RETURNS TABLE(status beckett.subscription_status)
    LANGUAGE sql
    AS $$
INSERT INTO beckett.subscriptions (group_name, name)
VALUES (_group_name, _name)
ON CONFLICT (group_name, name) DO NOTHING;

SELECT status
FROM beckett.subscriptions
WHERE group_name = _group_name
AND name = _name;
$$;


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
  PERFORM pg_advisory_xact_lock(beckett.stream_hash(_stream_name));

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
    AS $_$
BEGIN
  IF (TG_OP = 'UPDATE') THEN
    NEW.updated_at = now();
  END IF;

  IF (NEW.status = 'active' AND NEW.process_at IS NULL AND NEW.stream_version > NEW.stream_position) THEN
    NEW.process_at = now();
  END IF;

  IF (NEW.name != '$global' AND NEW.process_at IS NOT NULL AND NEW.reserved_until IS NULL) THEN
    PERFORM pg_notify('beckett:checkpoints', NEW.group_name);
  END IF;

  RETURN NEW;
END;
$_$;


--
-- Name: ensure_checkpoint_exists(text, text, text); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.ensure_checkpoint_exists(_group_name text, _name text, _stream_name text) RETURNS bigint
    LANGUAGE sql
    AS $$
WITH new_checkpoint AS (
  INSERT INTO beckett.checkpoints (group_name, name, stream_name)
  VALUES (_group_name, _name, _stream_name)
  ON CONFLICT (group_name, name, stream_name) DO NOTHING
  RETURNING 0 as stream_version
)
SELECT stream_version
FROM beckett.checkpoints
WHERE group_name = _group_name
AND name = _name
AND stream_name = _stream_name
UNION ALL
SELECT stream_version
FROM new_checkpoint;
$$;


--
-- Name: get_checkpoint_stream_version(text, text, text); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.get_checkpoint_stream_version(_group_name text, _name text, _stream_name text) RETURNS bigint
    LANGUAGE sql
    AS $$
SELECT stream_version
FROM beckett.checkpoints
WHERE group_name = _group_name
AND name = _name
AND stream_name = _stream_name;
$$;


--
-- Name: get_next_uninitialized_subscription(text); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.get_next_uninitialized_subscription(_group_name text) RETURNS TABLE(name text)
    LANGUAGE sql
    AS $$
SELECT name
FROM beckett.subscriptions
WHERE group_name = _group_name
AND status = 'uninitialized'
LIMIT 1;
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
WITH metric AS (
    SELECT count(*) as value
    FROM beckett.subscriptions s
    INNER JOIN beckett.checkpoints c ON s.group_name = c.group_name AND s.name = c.name
    WHERE s.status != 'uninitialized'
    AND c.status = 'failed'
    UNION ALL
    SELECT 0
)
SELECT value
FROM metric
LIMIT 1;
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
    INNER JOIN beckett.checkpoints c ON s.group_name = c.group_name AND s.name = c.name
    WHERE s.status = 'active'
    AND c.status = 'active'
    AND c.lagging = true
    GROUP BY c.group_name, c.name
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


--
-- Name: get_subscription_retry_count(); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.get_subscription_retry_count() RETURNS bigint
    LANGUAGE sql
    AS $$
WITH metric AS (
    SELECT count(*) as value
    FROM beckett.subscriptions s
    INNER JOIN beckett.checkpoints c ON s.group_name = c.group_name AND s.name = c.name
    WHERE s.status != 'uninitialized'
    AND c.status = 'retry'
    UNION ALL
    SELECT 0
)
SELECT value
FROM metric
LIMIT 1;
$$;


--
-- Name: lock_checkpoint(text, text, text); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.lock_checkpoint(_group_name text, _name text, _stream_name text) RETURNS TABLE(id bigint, stream_position bigint)
    LANGUAGE sql
    AS $$
SELECT id, stream_position
FROM beckett.checkpoints
WHERE group_name = _group_name
AND name = _name
AND stream_name = _stream_name
FOR UPDATE
SKIP LOCKED;
$$;


--
-- Name: pause_subscription(text, text); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.pause_subscription(_group_name text, _name text) RETURNS void
    LANGUAGE sql
    AS $$
UPDATE beckett.subscriptions
SET status = 'paused'
WHERE group_name = _group_name
AND name = _name;
$$;


--
-- Name: read_global_stream(bigint, integer, text, text[]); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.read_global_stream(_starting_global_position bigint, _count integer, _category text DEFAULT NULL::text, _types text[] DEFAULT NULL::text[]) RETURNS TABLE(id uuid, stream_name text, stream_position bigint, global_position bigint, type text, data jsonb, metadata jsonb, "timestamp" timestamp with time zone)
    LANGUAGE plpgsql
    AS $$
DECLARE
  _transaction_id xid8;
  _ending_global_position bigint;
BEGIN
  SELECT m.transaction_id
  INTO _transaction_id
  FROM beckett.messages m
  WHERE m.global_position = _starting_global_position
  AND m.archived = false;

  IF (_transaction_id IS NULL) THEN
    _transaction_id = '0'::xid8;
  END IF;

  _ending_global_position = _starting_global_position + _count;

  RETURN QUERY
    SELECT m.id,
           m.stream_name,
           m.stream_position,
           m.global_position,
           m.type,
           m.data,
           m.metadata,
           m.timestamp
    FROM beckett.messages m
    WHERE (m.transaction_id, m.global_position) > (_transaction_id, _starting_global_position)
    AND (m.global_position > _starting_global_position AND m.global_position <= _ending_global_position)
    AND m.transaction_id < pg_snapshot_xmin(pg_current_snapshot())
    AND m.archived = false
    AND (_category IS NULL OR beckett.stream_category(m.stream_name) = _category)
    AND (_types IS NULL OR m.type = ANY(_types))
    ORDER BY m.transaction_id, m.global_position;
END;
$$;


--
-- Name: read_index_batch(bigint, integer, text, text[]); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.read_index_batch(_starting_global_position bigint, _batch_size integer, _category text DEFAULT NULL::text, _types text[] DEFAULT NULL::text[]) RETURNS TABLE(stream_name text, stream_position bigint, global_position bigint, type text, tenant text, "timestamp" timestamp with time zone)
    LANGUAGE plpgsql
    AS $_$
DECLARE
  _transaction_id xid8;
  _ending_global_position bigint;
BEGIN
  SELECT m.transaction_id
  INTO _transaction_id
  FROM beckett.messages m
  WHERE m.global_position = _starting_global_position
  AND m.archived = false;

  IF (_transaction_id IS NULL) THEN
    _transaction_id = '0'::xid8;
  END IF;

  IF (_category IS NOT NULL OR _types IS NOT NULL) THEN
    _ending_global_position = _starting_global_position + _batch_size;
  END IF;

  RETURN QUERY
    SELECT m.stream_name,
           m.stream_position,
           m.global_position,
           m.type,
           m.metadata ->> '$tenant',
           m.timestamp
    FROM beckett.messages m
    WHERE (m.transaction_id, m.global_position) > (_transaction_id, _starting_global_position)
    AND m.transaction_id < pg_snapshot_xmin(pg_current_snapshot())
    AND m.archived = false
    AND (_ending_global_position IS NULL or m.global_position <= _ending_global_position)
    AND (_category IS NULL OR beckett.stream_category(m.stream_name) = _category)
    AND (_types IS NULL OR m.type = ANY(_types))
    ORDER BY m.transaction_id, m.global_position
    LIMIT _batch_size;
END;
$_$;


--
-- Name: read_stream(text, bigint, bigint, bigint, bigint, integer, boolean, text[]); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.read_stream(_stream_name text, _starting_stream_position bigint DEFAULT NULL::bigint, _ending_stream_position bigint DEFAULT NULL::bigint, _starting_global_position bigint DEFAULT NULL::bigint, _ending_global_position bigint DEFAULT NULL::bigint, _count integer DEFAULT NULL::integer, _read_forwards boolean DEFAULT true, _types text[] DEFAULT NULL::text[]) RETURNS TABLE(id uuid, stream_name text, stream_version bigint, stream_position bigint, global_position bigint, type text, data jsonb, metadata jsonb, "timestamp" timestamp with time zone)
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
    AND m.archived = false
    AND (_starting_global_position IS NULL OR m.global_position >= _starting_global_position)
    AND (_ending_global_position IS NULL OR m.global_position <= _ending_global_position)
    AND (_types IS NULL OR m.type = ANY(_types))
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
INSERT INTO beckett.checkpoints (stream_version, stream_position, group_name, name, stream_name)
SELECT c.stream_version, c.stream_position, c.group_name, c.name, c.stream_name
FROM unnest(_checkpoints) c
ON CONFLICT (group_name, name, stream_name) DO UPDATE
  SET stream_version = excluded.stream_version;
$$;


--
-- Name: record_stream_data(text[], timestamp with time zone[], text[]); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.record_stream_data(_category_names text[], _category_timestamps timestamp with time zone[], _tenants text[]) RETURNS void
    LANGUAGE sql
    AS $$
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


--
-- Name: recover_expired_checkpoint_reservations(text, integer); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.recover_expired_checkpoint_reservations(_group_name text, _batch_size integer) RETURNS void
    LANGUAGE sql
    AS $$
UPDATE beckett.checkpoints c
SET reserved_until = NULL
FROM (
    SELECT id
    FROM beckett.checkpoints
    WHERE group_name = _group_name
    AND reserved_until <= now()
    FOR UPDATE SKIP LOCKED
    LIMIT _batch_size
) as d
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
-- Name: reserve_next_available_checkpoint(text, interval); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.reserve_next_available_checkpoint(_group_name text, _reservation_timeout interval) RETURNS TABLE(id bigint, group_name text, name text, stream_name text, stream_position bigint, stream_version bigint, retry_attempts integer, status beckett.checkpoint_status)
    LANGUAGE sql
    AS $$
UPDATE beckett.checkpoints c
SET reserved_until = now() + _reservation_timeout
FROM (
  SELECT c.id
  FROM beckett.checkpoints c
  INNER JOIN beckett.subscriptions s ON c.group_name = s.group_name AND c.name = s.name
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


--
-- Name: resume_subscription(text, text); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.resume_subscription(_group_name text, _name text) RETURNS void
    LANGUAGE sql
    AS $$
UPDATE beckett.subscriptions
SET status = 'active'
WHERE group_name = _group_name
AND name = _name;

SELECT pg_notify('beckett:checkpoints', _group_name);
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
-- Name: set_subscription_status(text, text, beckett.subscription_status); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.set_subscription_status(_group_name text, _name text, _status beckett.subscription_status) RETURNS void
    LANGUAGE sql
    AS $$
UPDATE beckett.subscriptions
SET status = _status
WHERE group_name = _group_name
AND name = _name;
$$;


--
-- Name: set_subscription_to_active(text, text); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.set_subscription_to_active(_group_name text, _name text) RETURNS void
    LANGUAGE sql
    AS $_$
DELETE FROM beckett.checkpoints
WHERE group_name = _group_name
AND name = _name
AND stream_name = '$initializing';

UPDATE beckett.subscriptions
SET status = 'active'
WHERE group_name = _group_name
AND name = _name;
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
-- Name: stream_operations(); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.stream_operations() RETURNS trigger
    LANGUAGE plpgsql
    AS $_$
BEGIN
  IF NEW.type = '$stream_truncated' THEN
    UPDATE beckett.messages
    SET archived = TRUE
    WHERE stream_name = NEW.stream_name
    AND stream_position < NEW.stream_position
    AND archived = FALSE;
  END IF;

  IF NEW.type = '$stream_archived' THEN
    UPDATE beckett.messages
    SET archived = TRUE
    WHERE stream_name = NEW.stream_name
    AND stream_position < NEW.stream_position
    AND archived = FALSE;
  END IF;

  RETURN NEW;
END;
$_$;


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
-- Name: categories; Type: TABLE; Schema: beckett; Owner: -
--

CREATE TABLE beckett.categories (
    name text NOT NULL,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);


--
-- Name: checkpoints; Type: TABLE; Schema: beckett; Owner: -
--

CREATE TABLE beckett.checkpoints (
    id bigint NOT NULL,
    stream_version bigint DEFAULT 0 NOT NULL,
    stream_position bigint DEFAULT 0 NOT NULL,
    created_at timestamp with time zone DEFAULT now(),
    updated_at timestamp with time zone DEFAULT now(),
    process_at timestamp with time zone,
    reserved_until timestamp with time zone,
    lagging boolean GENERATED ALWAYS AS ((stream_version > stream_position)) STORED,
    status beckett.checkpoint_status DEFAULT 'active'::beckett.checkpoint_status NOT NULL,
    group_name text NOT NULL,
    name text NOT NULL,
    stream_name text NOT NULL,
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
-- Name: subscriptions; Type: TABLE; Schema: beckett; Owner: -
--

CREATE TABLE beckett.subscriptions (
    group_name text NOT NULL,
    name text NOT NULL,
    status beckett.subscription_status DEFAULT 'uninitialized'::beckett.subscription_status NOT NULL
);


--
-- Name: tenants; Type: TABLE; Schema: beckett; Owner: -
--

CREATE TABLE beckett.tenants (
    tenant text NOT NULL
);


--
-- Name: messages_active; Type: TABLE ATTACH; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.messages ATTACH PARTITION beckett.messages_active FOR VALUES IN (false);


--
-- Name: messages_archived; Type: TABLE ATTACH; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.messages ATTACH PARTITION beckett.messages_archived FOR VALUES IN (true);


--
-- Name: categories categories_pkey; Type: CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.categories
    ADD CONSTRAINT categories_pkey PRIMARY KEY (name);


--
-- Name: checkpoints checkpoints_group_name_name_stream_name_key; Type: CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.checkpoints
    ADD CONSTRAINT checkpoints_group_name_name_stream_name_key UNIQUE (group_name, name, stream_name);


--
-- Name: checkpoints checkpoints_pkey; Type: CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.checkpoints
    ADD CONSTRAINT checkpoints_pkey PRIMARY KEY (id);


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
-- Name: subscriptions subscriptions_pkey; Type: CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.subscriptions
    ADD CONSTRAINT subscriptions_pkey PRIMARY KEY (group_name, name);


--
-- Name: tenants tenants_pkey; Type: CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.tenants
    ADD CONSTRAINT tenants_pkey PRIMARY KEY (tenant);


--
-- Name: ix_checkpoints_metrics; Type: INDEX; Schema: beckett; Owner: -
--

CREATE INDEX ix_checkpoints_metrics ON beckett.checkpoints USING btree (status, lagging, group_name, name);


--
-- Name: ix_checkpoints_reserved; Type: INDEX; Schema: beckett; Owner: -
--

CREATE INDEX ix_checkpoints_reserved ON beckett.checkpoints USING btree (group_name, reserved_until) WHERE (reserved_until IS NOT NULL);


--
-- Name: ix_checkpoints_to_process; Type: INDEX; Schema: beckett; Owner: -
--

CREATE INDEX ix_checkpoints_to_process ON beckett.checkpoints USING btree (group_name, process_at, reserved_until) WHERE ((process_at IS NOT NULL) AND (reserved_until IS NULL));


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

CREATE INDEX ix_subscriptions_active ON beckett.subscriptions USING btree (group_name, name, status) WHERE (status = 'active'::beckett.subscription_status);


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
-- Name: messages stream_operations; Type: TRIGGER; Schema: beckett; Owner: -
--

CREATE TRIGGER stream_operations BEFORE INSERT ON beckett.messages FOR EACH ROW EXECUTE FUNCTION beckett.stream_operations();


--
-- PostgreSQL database dump complete
--

