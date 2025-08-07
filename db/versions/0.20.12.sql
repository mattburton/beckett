-- Beckett v0.20.12

-- inline append query
DROP FUNCTION IF EXISTS beckett.append_to_stream(text, bigint, beckett.message[]);

-- utility function to enable raising exceptions from inline queries
CREATE OR REPLACE FUNCTION beckett.assert_condition(
  _condition boolean,
  _message text
)
  RETURNS boolean
  IMMUTABLE
  LANGUAGE plpgsql
AS
$$
BEGIN
  IF NOT _condition THEN
    RAISE EXCEPTION '%', _message;
  END IF;
  RETURN TRUE;
END;
$$;

-- correlated message query performance fix
DROP INDEX IF EXISTS beckett.ix_messages_active_correlation_id;

CREATE INDEX ix_messages_active_correlation_id_global_position
  ON beckett.messages_active ((metadata ->> '$correlation_id'::text), global_position)
  WHERE ((metadata ->> '$correlation_id'::text) IS NOT NULL);

-- subscription utility functions
CREATE OR REPLACE FUNCTION beckett.delete_subscription(
  _group_name text,
  _name text
)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
DECLARE
  _rows_deleted integer;
BEGIN
  LOOP
    DELETE FROM beckett.checkpoints
    WHERE id IN (
      SELECT id
      FROM beckett.checkpoints
      WHERE group_name = _group_name
      AND name = _name
      LIMIT 500
    );

    GET DIAGNOSTICS _rows_deleted = ROW_COUNT;
    EXIT WHEN _rows_deleted = 0;
  END LOOP;

  DELETE FROM beckett.subscriptions
  WHERE group_name = _group_name
  AND name = _name;
END;
$$;

CREATE OR REPLACE FUNCTION beckett.move_subscription(
  _group_name text,
  _name text,
  _new_group_name text
)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
DECLARE
  _rows_updated integer;
BEGIN
  UPDATE beckett.subscriptions
  SET group_name = _new_group_name
  WHERE group_name = _group_name
  AND name = _name;

  LOOP
    UPDATE beckett.checkpoints
    SET group_name = _new_group_name
    WHERE id IN (
      SELECT id
      FROM beckett.checkpoints
      WHERE group_name = _group_name
      AND name = _name
      LIMIT 500
    );

    GET DIAGNOSTICS _rows_updated = ROW_COUNT;
    EXIT WHEN _rows_updated = 0;
  END LOOP;
END;
$$;

CREATE OR REPLACE FUNCTION beckett.rename_subscription(
  _group_name text,
  _name text,
  _new_name text
)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
DECLARE
  _rows_updated integer;
BEGIN
  UPDATE beckett.subscriptions
  SET name = _new_name
  WHERE group_name = _group_name
  AND name = _name;

  LOOP
    UPDATE beckett.checkpoints
    SET name = _new_name
    WHERE id IN (
      SELECT id
      FROM beckett.checkpoints
      WHERE group_name = _group_name
      AND name = _name
      LIMIT 500
    );

    GET DIAGNOSTICS _rows_updated = ROW_COUNT;
    EXIT WHEN _rows_updated = 0;
  END LOOP;
END;
$$;

CREATE OR REPLACE FUNCTION beckett.replay_subscription(
  _group_name text,
  _name text
)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
DECLARE
  _replay_target_position bigint;
  _rows_updated integer;
BEGIN
  SELECT coalesce(max(m.global_position), 0)
  INTO _replay_target_position
  FROM beckett.checkpoints c
  INNER JOIN beckett.messages_active m ON c.stream_name = m.stream_name AND c.stream_version = m.stream_position
  WHERE c.group_name = _group_name
  AND c.name = _name;

  LOOP
    UPDATE beckett.checkpoints
    SET stream_position = 0
    WHERE id IN (
      SELECT id
      FROM beckett.checkpoints
      WHERE group_name = _group_name
      AND name = _name
      AND stream_position > 0
      LIMIT 500
    );

    GET DIAGNOSTICS _rows_updated = ROW_COUNT;
    EXIT WHEN _rows_updated = 0;
  END LOOP;

  UPDATE beckett.subscriptions
  SET status = 'replay',
      replay_target_position = _replay_target_position
  WHERE group_name = _group_name
  AND name = _name;
END;
$$;

CREATE OR REPLACE FUNCTION beckett.reset_subscription(
  _group_name text,
  _name text
)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
DECLARE
  _rows_deleted integer;
BEGIN
  LOOP
    DELETE FROM beckett.checkpoints
    WHERE group_name = _group_name AND name = _name
    AND id IN (
      SELECT id FROM beckett.checkpoints
      WHERE group_name = _group_name AND name = _name
      LIMIT 500
    );

    GET DIAGNOSTICS _rows_deleted = ROW_COUNT;
    EXIT WHEN _rows_deleted = 0;
  END LOOP;

  UPDATE beckett.subscriptions
  SET status = 'uninitialized', replay_target_position = null
  WHERE group_name = _group_name
  AND name = _name;

  INSERT INTO beckett.checkpoints (group_name, name, stream_name)
  VALUES (_group_name, _name, '$initializing')
  ON CONFLICT (group_name, name, stream_name) DO UPDATE
    SET stream_version = 0,
        stream_position = 0;
END;
$$;
