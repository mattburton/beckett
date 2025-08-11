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
	subscription_id bigint,
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
-- Name: assert_condition(boolean, text); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.assert_condition(_condition boolean, _message text) RETURNS boolean
    LANGUAGE plpgsql IMMUTABLE
    AS $$
BEGIN
  IF NOT _condition THEN
    RAISE EXCEPTION '%', _message;
  END IF;
  RETURN TRUE;
END;
$$;


-- Checkpoint preprocessor function removed in v0.23.1
-- Checkpoint management is now handled entirely by application queries


--
-- Name: delete_subscription(text, text); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.delete_subscription(_group_name text, _name text) RETURNS void
    LANGUAGE plpgsql
    AS $$
DECLARE
  _subscription_id bigint;
  _rows_deleted integer;
BEGIN
  SELECT s.id INTO _subscription_id
  FROM beckett.subscriptions s
  INNER JOIN beckett.subscription_groups sg ON s.subscription_group_id = sg.id
  WHERE sg.name = _group_name
  AND s.name = _name;

  IF _subscription_id IS NOT NULL THEN
    LOOP
      DELETE FROM beckett.checkpoints
      WHERE id IN (
        SELECT id
        FROM beckett.checkpoints
        WHERE subscription_id = _subscription_id
        LIMIT 500
      );

      GET DIAGNOSTICS _rows_deleted = ROW_COUNT;
      EXIT WHEN _rows_deleted = 0;
    END LOOP;

    DELETE FROM beckett.subscriptions WHERE id = _subscription_id;
  END IF;
END;
$$;


--
-- Name: move_subscription(text, text, text); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.move_subscription(_group_name text, _name text, _new_group_name text) RETURNS void
    LANGUAGE plpgsql
    AS $$
DECLARE
  _subscription_id bigint;
  _new_subscription_group_id bigint;
BEGIN
  SELECT s.id INTO _subscription_id
  FROM beckett.subscriptions s
  INNER JOIN beckett.subscription_groups sg ON s.subscription_group_id = sg.id
  WHERE sg.name = _group_name
  AND s.name = _name;

  SELECT id INTO _new_subscription_group_id
  FROM beckett.subscription_groups
  WHERE name = _new_group_name;

  IF _subscription_id IS NOT NULL AND _new_subscription_group_id IS NOT NULL THEN
    UPDATE beckett.subscriptions
    SET subscription_group_id = _new_subscription_group_id
    WHERE id = _subscription_id;
  END IF;
END;
$$;


--
-- Name: rename_subscription(text, text, text); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.rename_subscription(_group_name text, _name text, _new_name text) RETURNS void
    LANGUAGE plpgsql
    AS $$
DECLARE
  _subscription_id bigint;
BEGIN
  SELECT s.id INTO _subscription_id
  FROM beckett.subscriptions s
  INNER JOIN beckett.subscription_groups sg ON s.subscription_group_id = sg.id
  WHERE sg.name = _group_name
  AND s.name = _name;

  IF _subscription_id IS NOT NULL THEN
    UPDATE beckett.subscriptions
    SET name = _new_name
    WHERE id = _subscription_id;
  END IF;
END;
$$;


--
-- Name: replay_subscription(text, text); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.replay_subscription(_group_name text, _name text) RETURNS void
    LANGUAGE plpgsql
    AS $$
DECLARE
  _subscription_id bigint;
  _replay_target_position bigint;
  _rows_updated integer;
BEGIN
  SELECT s.id INTO _subscription_id
  FROM beckett.subscriptions s
  INNER JOIN beckett.subscription_groups sg ON s.subscription_group_id = sg.id
  WHERE sg.name = _group_name
  AND s.name = _name;

  IF _subscription_id IS NOT NULL THEN
    SELECT coalesce(max(m.global_position), 0)
    INTO _replay_target_position
    FROM beckett.checkpoints c
    INNER JOIN beckett.messages_active m ON c.stream_name = m.stream_name AND c.stream_version = m.stream_position
    WHERE c.subscription_id = _subscription_id;

    LOOP
      UPDATE beckett.checkpoints
      SET stream_position = 0
      WHERE id IN (
        SELECT id
        FROM beckett.checkpoints
        WHERE subscription_id = _subscription_id
        AND stream_position > 0
        LIMIT 500
      );

      GET DIAGNOSTICS _rows_updated = ROW_COUNT;
      EXIT WHEN _rows_updated = 0;
    END LOOP;

    UPDATE beckett.subscriptions
    SET status = 'replay',
        replay_target_position = _replay_target_position
    WHERE id = _subscription_id;
  END IF;
END;
$$;


--
-- Name: reset_subscription(text, text); Type: FUNCTION; Schema: beckett; Owner: -
--

CREATE FUNCTION beckett.reset_subscription(_group_name text, _name text) RETURNS void
    LANGUAGE plpgsql
    AS $_$
DECLARE
  _subscription_id bigint;
  _rows_deleted integer;
BEGIN
  SELECT s.id INTO _subscription_id
  FROM beckett.subscriptions s
  INNER JOIN beckett.subscription_groups sg ON s.subscription_group_id = sg.id
  WHERE sg.name = _group_name
  AND s.name = _name;

  IF _subscription_id IS NOT NULL THEN
    LOOP
      DELETE FROM beckett.checkpoints
      WHERE subscription_id = _subscription_id
      AND id IN (
        SELECT id FROM beckett.checkpoints
        WHERE subscription_id = _subscription_id
        LIMIT 500
      );

      GET DIAGNOSTICS _rows_deleted = ROW_COUNT;
      EXIT WHEN _rows_deleted = 0;
    END LOOP;

    UPDATE beckett.subscriptions
    SET status = 'uninitialized',
        replay_target_position = NULL
    WHERE id = _subscription_id;

    INSERT INTO beckett.checkpoints (subscription_id, stream_name)
    VALUES (_subscription_id, '$initializing')
    ON CONFLICT (subscription_id, stream_name) DO UPDATE
      SET stream_version = 0,
          stream_position = 0;
  END IF;
END;
$_$;


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
    subscription_id bigint NOT NULL,
    stream_position bigint DEFAULT 0 NOT NULL,
    created_at timestamp with time zone DEFAULT now(),
    updated_at timestamp with time zone DEFAULT now(),
    retry_attempts integer DEFAULT 0 NOT NULL,
    status beckett.checkpoint_status DEFAULT 'active'::beckett.checkpoint_status NOT NULL,
    stream_name text NOT NULL,
    retries beckett.retry[]
);


--
-- Name: checkpoints_ready; Type: TABLE; Schema: beckett; Owner: -
--

CREATE TABLE beckett.checkpoints_ready (
    id bigint NOT NULL,
    process_at timestamp with time zone DEFAULT now() NOT NULL,
    subscription_group_name text NOT NULL,
    target_stream_version bigint NOT NULL
);


--
-- Name: checkpoints_reserved; Type: TABLE; Schema: beckett; Owner: -
--

CREATE TABLE beckett.checkpoints_reserved (
    id bigint NOT NULL,
    reserved_until timestamp with time zone NOT NULL,
    target_stream_version bigint NOT NULL
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
-- Name: recurring_messages; Type: TABLE; Schema: beckett; Owner: -
--

CREATE TABLE beckett.recurring_messages (
    name text NOT NULL,
    cron_expression text NOT NULL,
    time_zone_id text NOT NULL,
    stream_name text NOT NULL,
    type text NOT NULL,
    data jsonb NOT NULL,
    metadata jsonb NOT NULL,
    next_occurrence timestamp with time zone,
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
-- Name: subscription_groups; Type: TABLE; Schema: beckett; Owner: -
--

CREATE TABLE beckett.subscription_groups (
    id bigint NOT NULL,
    name text NOT NULL
);


--
-- Name: subscription_groups_id_seq; Type: SEQUENCE; Schema: beckett; Owner: -
--

ALTER TABLE beckett.subscription_groups ALTER COLUMN id ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME beckett.subscription_groups_id_seq
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
    id bigint NOT NULL,
    subscription_group_id bigint NOT NULL,
    name text NOT NULL,
    status beckett.subscription_status DEFAULT 'uninitialized'::beckett.subscription_status NOT NULL,
    replay_target_position bigint
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
-- Name: checkpoints checkpoints_pkey; Type: CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.checkpoints
    ADD CONSTRAINT checkpoints_pkey PRIMARY KEY (id);


--
-- Name: checkpoints checkpoints_subscription_id_stream_name_key; Type: CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.checkpoints
    ADD CONSTRAINT checkpoints_subscription_id_stream_name_key UNIQUE (subscription_id, stream_name);


--
-- Name: checkpoints_ready checkpoints_ready_pkey; Type: CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.checkpoints_ready
    ADD CONSTRAINT checkpoints_ready_pkey PRIMARY KEY (id);


--
-- Name: checkpoints_reserved checkpoints_reserved_pkey; Type: CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.checkpoints_reserved
    ADD CONSTRAINT checkpoints_reserved_pkey PRIMARY KEY (id);


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
-- Name: recurring_messages recurring_messages_pkey; Type: CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.recurring_messages
    ADD CONSTRAINT recurring_messages_pkey PRIMARY KEY (name);


--
-- Name: scheduled_messages scheduled_messages_pkey; Type: CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.scheduled_messages
    ADD CONSTRAINT scheduled_messages_pkey PRIMARY KEY (id);


--
-- Name: subscription_groups subscription_groups_name_key; Type: CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.subscription_groups
    ADD CONSTRAINT subscription_groups_name_key UNIQUE (name);


--
-- Name: subscription_groups subscription_groups_pkey; Type: CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.subscription_groups
    ADD CONSTRAINT subscription_groups_pkey PRIMARY KEY (id);


--
-- Name: subscriptions subscriptions_pkey; Type: CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.subscriptions
    ADD CONSTRAINT subscriptions_pkey PRIMARY KEY (id);


--
-- Name: subscriptions subscriptions_subscription_group_id_name_key; Type: CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.subscriptions
    ADD CONSTRAINT subscriptions_subscription_group_id_name_key UNIQUE (subscription_group_id, name);


--
-- Name: tenants tenants_pkey; Type: CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.tenants
    ADD CONSTRAINT tenants_pkey PRIMARY KEY (tenant);


--
-- Name: ix_checkpoints_metrics; Type: INDEX; Schema: beckett; Owner: -
--

CREATE INDEX ix_checkpoints_metrics ON beckett.checkpoints USING btree (status, subscription_id);


-- ix_checkpoints_reserved removed in v0.23.3 - reservations now handled by checkpoints_reserved table


--
-- Name: ix_checkpoints_subscription_id; Type: INDEX; Schema: beckett; Owner: -
--

CREATE INDEX ix_checkpoints_subscription_id ON beckett.checkpoints USING btree (subscription_id);


-- ix_checkpoints_to_process removed in v0.23.3 - scheduling now handled by checkpoints_ready table


--
-- Name: ix_checkpoints_ready_group_process_at; Type: INDEX; Schema: beckett; Owner: -
--

CREATE INDEX ix_checkpoints_ready_group_process_at ON beckett.checkpoints_ready USING btree (subscription_group_name, process_at);


--
-- Name: ix_checkpoints_reserved_reserved_until; Type: INDEX; Schema: beckett; Owner: -
--

CREATE INDEX ix_checkpoints_reserved_reserved_until ON beckett.checkpoints_reserved USING btree (reserved_until);


--
-- Name: ix_messages_active_correlation_id_global_position; Type: INDEX; Schema: beckett; Owner: -
--

CREATE INDEX ix_messages_active_correlation_id_global_position ON beckett.messages_active USING btree (((metadata ->> '$correlation_id'::text)), global_position) WHERE ((metadata ->> '$correlation_id'::text) IS NOT NULL);


--
-- Name: ix_messages_active_global_read_stream; Type: INDEX; Schema: beckett; Owner: -
--

CREATE INDEX ix_messages_active_global_read_stream ON beckett.messages_active USING btree (transaction_id, global_position, archived);


--
-- Name: ix_messages_active_tenant_stream_category; Type: INDEX; Schema: beckett; Owner: -
--

CREATE INDEX ix_messages_active_tenant_stream_category ON beckett.messages_active USING btree (((metadata ->> '$tenant'::text)), beckett.stream_category(stream_name)) WHERE ((metadata ->> '$tenant'::text) IS NOT NULL);


--
-- Name: ix_recurring_messages_next_occurrence; Type: INDEX; Schema: beckett; Owner: -
--

CREATE INDEX ix_recurring_messages_next_occurrence ON beckett.recurring_messages USING btree (next_occurrence) WHERE (next_occurrence IS NOT NULL);


--
-- Name: ix_scheduled_messages_deliver_at; Type: INDEX; Schema: beckett; Owner: -
--

CREATE INDEX ix_scheduled_messages_deliver_at ON beckett.scheduled_messages USING btree (deliver_at);


--
-- Name: ix_subscriptions_reservation_candidates; Type: INDEX; Schema: beckett; Owner: -
--

CREATE INDEX ix_subscriptions_reservation_candidates ON beckett.subscriptions USING btree (subscription_group_id, name, status) WHERE ((status = 'active'::beckett.subscription_status) OR (status = 'replay'::beckett.subscription_status));


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


-- Checkpoint preprocessor trigger removed in v0.23.1
-- Checkpoint management is now handled entirely by application queries


--
-- Name: checkpoints checkpoints_subscription_id_fkey; Type: FK CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.checkpoints
    ADD CONSTRAINT checkpoints_subscription_id_fkey FOREIGN KEY (subscription_id) REFERENCES beckett.subscriptions(id) ON DELETE CASCADE;


--
-- Name: subscriptions subscriptions_subscription_group_id_fkey; Type: FK CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.subscriptions
    ADD CONSTRAINT subscriptions_subscription_group_id_fkey FOREIGN KEY (subscription_group_id) REFERENCES beckett.subscription_groups(id) ON DELETE CASCADE;


--
-- Name: checkpoints_ready checkpoints_ready_id_fkey; Type: FK CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.checkpoints_ready
    ADD CONSTRAINT checkpoints_ready_id_fkey FOREIGN KEY (id) REFERENCES beckett.checkpoints(id) ON DELETE CASCADE;


--
-- Name: checkpoints_reserved checkpoints_reserved_id_fkey; Type: FK CONSTRAINT; Schema: beckett; Owner: -
--

ALTER TABLE ONLY beckett.checkpoints_reserved
    ADD CONSTRAINT checkpoints_reserved_id_fkey FOREIGN KEY (id) REFERENCES beckett.checkpoints(id) ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--

