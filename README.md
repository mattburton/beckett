# Beckett

Building blocks for event sourcing and message-based applications:

- Subscriptions - subscribe to messages and process them in order by stream
  - Event handlers, projections, etc... - add asynchronous, event-driven behavior to your applications
  - Horizontal scalability - use auto-scaling to have as many nodes as needed processing messages in parallel where the work is distributed automatically across all available nodes without needing to manage the distribution by way of consumer groups or similar mechanisms
  - Retries - built-in retry support for failed messages - since messages are processed in order by stream per subscription, a failed message only blocks a single stream for a subscription at a time and the rest of the streams can continue processing for that subscription
- Scheduled Messages - schedule messages to be sent at a future time with cancellation support
- Open Telemetry - built-in support to provide tracing and metrics
- Dashboard - browse messages, retry failed subscriptions, and more
- Test Helpers - easily write unit tests for Given-When-Then style specifications, fakes to simplify testing without mocks, and more
- Bring Your Own Event Store - Beckett provides a simple Postgres-based message store or use your own by implementing the `IMessageStorage` interface
