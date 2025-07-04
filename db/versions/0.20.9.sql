-- Beckett v0.20.9

-- 1) update replay handling - Beckett will now track the replay target position during subscription initialization so
-- that it's accurate based on the streams that need to be replayed vs just being the global position of the
-- subscription group which could lead to the subscription being stuck in replay status until that global position is
-- hit for that subscription
-- 2) drop functions that are no longer needed
DROP FUNCTION IF EXISTS beckett.add_or_update_subscription(text, text);
DROP FUNCTION IF EXISTS beckett.get_next_uninitialized_subscription(text);
DROP FUNCTION IF EXISTS beckett.set_subscription_to_active(text, text);
DROP FUNCTION IF EXISTS beckett.set_subscription_to_replay(text, text, bigint);
DROP FUNCTION IF EXISTS beckett.set_subscription_status(text, text, beckett.subscription_status);
DROP FUNCTION IF EXISTS beckett.schedule_message(text, beckett.scheduled_message);
DROP FUNCTION IF EXISTS beckett.get_scheduled_messages_to_deliver(int);
DROP FUNCTION IF EXISTS beckett.cancel_scheduled_message(uuid);
DROP FUNCTION IF EXISTS beckett.update_checkpoint_position(bigint, bigint, timestamp with time zone);
DROP FUNCTION IF EXISTS beckett.update_system_checkpoint_position(bigint, bigint);
DROP FUNCTION IF EXISTS beckett.ensure_checkpoint_exists(text, text, text);
DROP FUNCTION IF EXISTS beckett.lock_checkpoint(text, text, text);
DROP FUNCTION IF EXISTS beckett.record_checkpoint_error(bigint, bigint, beckett.checkpoint_status, int, jsonb, timestamp with time zone);
DROP FUNCTION IF EXISTS beckett.record_checkpoints(beckett.checkpoint[]);
DROP FUNCTION IF EXISTS beckett.recover_expired_checkpoint_reservations(text, int);
DROP FUNCTION IF EXISTS beckett.release_checkpoint_reservation(bigint);
DROP FUNCTION IF EXISTS beckett.reserve_next_available_checkpoint(text, interval, boolean, boolean, boolean);
DROP FUNCTION IF EXISTS beckett.read_stream(text, bigint, bigint, bigint, bigint, integer, boolean, text[]);
DROP FUNCTION IF EXISTS beckett.read_global_stream(bigint, int);
DROP FUNCTION IF EXISTS beckett.record_stream_data(text[], timestamp with time zone[], text[]);
DROP FUNCTION IF EXISTS beckett.get_subscription_lag_count();
DROP FUNCTION IF EXISTS beckett.get_subscription_retry_count();
DROP FUNCTION IF EXISTS beckett.get_subscription_failed_count();
DROP FUNCTION IF EXISTS beckett.delete_subscription(text, text);
DROP FUNCTION IF EXISTS beckett.move_subscription(text, text, text);
DROP FUNCTION IF EXISTS beckett.rename_subscription(text, text, text);
DROP FUNCTION IF EXISTS beckett.reset_subscription(text, text);

DROP TYPE IF EXISTS beckett.scheduled_message;
