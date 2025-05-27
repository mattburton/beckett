-- Beckett v0.19.0 - spring cleanup
DROP FUNCTION IF EXISTS beckett.pause_subscription(text, text);
DROP FUNCTION IF EXISTS beckett.resume_subscription(text, text);
DROP FUNCTION IF EXISTS beckett.get_subscription_metrics();
DROP FUNCTION IF EXISTS beckett.schedule_checkpoints(bigint[], timestamp with time zone);
DROP FUNCTION IF EXISTS beckett.skip_checkpoint_position(bigint);
DROP FUNCTION IF EXISTS beckett.stream_operations();
DROP FUNCTION IF EXISTS beckett.get_checkpoint_stream_version(text, text, text);
