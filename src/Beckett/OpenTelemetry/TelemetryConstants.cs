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
        public const string AppendToStream = $"{ActivitySource.Name}.{nameof(AppendToStream)}";
        public const string ReadStream = $"{ActivitySource.Name}.{nameof(ReadStream)}";
        public const string ReadStreamBatch = $"{ActivitySource.Name}.{nameof(ReadStreamBatch)}";
        public const string ScheduleMessage = $"{ActivitySource.Name}.{nameof(ScheduleMessage)}";
        public const string SessionAppendToStream = $"{ActivitySource.Name}.{nameof(SessionAppendToStream)}";
        public const string SessionSaveChanges = $"{ActivitySource.Name}.{nameof(SessionSaveChanges)}";
    }

    public static class Streams
    {
        public const string Name = "stream_name";
    }

    public static class Subscription
    {
        public const string GroupName = "subscription.group_name";
        public const string Name = "subscription.name";
        public const string Category = "subscription.category";
        public const string Handler = "subscription.handler";
    }

    public static class Message
    {
        public const string Id = "message.id";
        public const string CausationId = "message.causation_id";
        public const string StreamName = "message.stream_name";
        public const string GlobalPosition = "message.global_position";
        public const string StreamPosition = "message.stream_position";
        public const string Type = "message.type";
    }
}
