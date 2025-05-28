-- Beckett v0.19.0 - spring cleanup
DROP TRIGGER IF EXISTS stream_operations ON beckett.messages;
DROP FUNCTION IF EXISTS beckett.pause_subscription(text, text);
DROP FUNCTION IF EXISTS beckett.resume_subscription(text, text);
DROP FUNCTION IF EXISTS beckett.get_subscription_metrics();
DROP FUNCTION IF EXISTS beckett.schedule_checkpoints(bigint[], timestamp with time zone);
DROP FUNCTION IF EXISTS beckett.skip_checkpoint_position(bigint);
DROP FUNCTION IF EXISTS beckett.stream_operations();
DROP FUNCTION IF EXISTS beckett.get_checkpoint_stream_version(text, text, text);

-- utility function to delete a subscription
CREATE OR REPLACE FUNCTION beckett.delete_subscription(_group_name text, _name text)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
BEGIN
  DELETE FROM beckett.checkpoints
  WHERE group_name = _group_name
  AND name = _name;

  DELETE FROM beckett.subscriptions
  WHERE group_name = _group_name
  AND name = _name;
END;
$$;
