-- add 'unknown' subscription status and function to set status
ALTER TYPE beckett.subscription_status ADD VALUE 'unknown';

CREATE OR REPLACE FUNCTION beckett.set_subscription_status(
  _group_name text,
  _name text,
  _status beckett.subscription_status
)
  RETURNS void
  LANGUAGE sql
AS
$$
UPDATE beckett.subscriptions
SET status = _status
WHERE group_name = _group_name
AND name = _name;
$$;
