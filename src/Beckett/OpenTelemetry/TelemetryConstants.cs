namespace Beckett.OpenTelemetry;

public static class TelemetryConstants
{
    public static class ActivitySource
    {
        public const string Name = "Beckett";
    }

    public static class Metrics
    {
        public const string SubscriptionLag = "beckett.subscriptions.lag";
        public const string SubscriptionRetryCount = "beckett.subscriptions.retry";
        public const string SubscriptionFailedCount = "beckett.subscriptions.failed";
    }

    public static class Activities
    {
        public const string AppendToStream = nameof(AppendToStream);
        public const string ReadStream = nameof(ReadStream);
        public const string HandleMessage = nameof(HandleMessage);
    }

    public static class Streams
    {
        public const string Topic = "topic";
        public const string StreamId = "stream_id";
    }

    public static class Application
    {
        public const string Name = "application.name";
    }

    public static class Subscription
    {
        public const string Name = "subscription.name";
        public const string Topic = "subscription.topic";
        public const string Handler = "subscription.handler";
    }

    public static class Message
    {
        public const string Id = "message.id";
        public const string CausationId = "message.causation_id";
        public const string Topic = "message.topic";
        public const string StreamId = "message.stream_id";
        public const string GlobalPosition = "message.global_position";
        public const string StreamPosition = "message.stream_position";
        public const string Type = "message.type";
    }
}
