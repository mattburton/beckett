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
-- Name: subscription_status; Type: TYPE; Schema: beckett; Owner: -
--

CREATE TYPE beckett.subscription_status AS ENUM (
    'uninitialized',
    'active',
    'paused',
    'unknown',
    'replay',
    'backfill'
);


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
    PERFORM pg_notify('beckett:checkpoints', NEW.group_name);
  END IF;

  RETURN NEW;
END;
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


SET default_tablespace = '';

SET default_table_access_method = heap;

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
    retry_attempts integer GENERATED ALWAYS AS (COALESCE(array_length(retries, 1), 0)) STORED,
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
-- Name: subscriptions; Type: TABLE; Schema: beckett; Owner: -
--

CREATE TABLE beckett.subscriptions (
    group_name text NOT NULL,
    name text NOT NULL,
    status beckett.subscription_status DEFAULT 'uninitialized'::beckett.subscription_status NOT NULL,
    replay_target_position bigint
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
-- Name: ix_subscriptions_reservation_candidates; Type: INDEX; Schema: beckett; Owner: -
--

CREATE INDEX ix_subscriptions_reservation_candidates ON beckett.subscriptions USING btree (group_name, name, status) WHERE ((status = 'active'::beckett.subscription_status) OR (status = 'replay'::beckett.subscription_status));


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
-- PostgreSQL database dump complete
--

