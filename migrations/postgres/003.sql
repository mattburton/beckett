CREATE ROLE beckett;
GRANT USAGE ON SCHEMA __schema__ to beckett;
GRANT SELECT, INSERT ON ALL TABLES IN SCHEMA __schema__ TO beckett;
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA __schema__ TO beckett;

GRANT UPDATE,DELETE ON __schema__.subscription_streams TO beckett;
GRANT UPDATE,DELETE ON __schema__.subscriptions TO beckett;
GRANT UPDATE,DELETE ON __schema__.scheduled_events TO beckett;
