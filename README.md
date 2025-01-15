# Beckett

Building blocks for event sourcing and message-based applications:

- Subscriptions - subscribe to messages and process them in order by stream
  - Message handlers, projections, etc... - add asynchronous, event-driven behavior to your applications
  - Horizontal scalability - use auto-scaling to have multiple nodes processing messages in parallel
  - Retries - built-in retry support when errors occur - since messages are processed in order by stream per
    subscription, an error only blocks a single stream for a subscription at a time, and the rest of the streams can
    continue processing
- Projections - quickly and easily build persisted state views
- Scheduled Messages - schedule messages to be sent at a future time with cancellation support
- Open Telemetry - built-in support to provide tracing and metrics
- Dashboard - browse messages, retry failures, and more
- Bring Your Own Message Store - Beckett provides a simple Postgres-based message store or use your own by implementing
  the `IMessageStorage` interface
