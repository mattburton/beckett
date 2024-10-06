using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Text.Json;
using Beckett.Database;
using Beckett.Messages;
using Beckett.Subscriptions;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Context.Propagation;

namespace Beckett.OpenTelemetry;

[SuppressMessage("ReSharper", "ExplicitCallerInfoArgument")]
public class Instrumentation : IInstrumentation, IDisposable
{
    private readonly ActivitySource _activitySource;
    private readonly IPostgresDatabase _database;
    private readonly ILogger<Instrumentation> _logger;
    private readonly Meter? _meter;
    private readonly BeckettOptions _options;
    private readonly TextMapPropagator _propagator;

    public Instrumentation(
        IPostgresDatabase database,
        IMeterFactory meterFactory,
        BeckettOptions options,
        ILogger<Instrumentation> logger
    )
    {
        _database = database;
        _options = options;
        _logger = logger;

        var version = typeof(Instrumentation).Assembly.GetName().Version?.ToString();

        _activitySource = new ActivitySource(TelemetryConstants.ActivitySource.Name, version);

        _propagator = Propagators.DefaultTextMapPropagator;

        if (!options.Subscriptions.Enabled)
        {
            return;
        }

        _meter = meterFactory.Create(TelemetryConstants.ActivitySource.Name, version);

        _meter.CreateObservableGauge(TelemetryConstants.Metrics.SubscriptionLag, GetSubscriptionLag);

        _meter.CreateObservableGauge(TelemetryConstants.Metrics.SubscriptionRetryCount, GetSubscriptionRetryCount);

        _meter.CreateObservableGauge(TelemetryConstants.Metrics.SubscriptionFailedCount, GetSubscriptionFailedCount);
    }

    public void Dispose()
    {
        _activitySource.Dispose();

        _meter?.Dispose();

        GC.SuppressFinalize(this);
    }

    public Activity? StartAppendToStreamActivity(string streamName, Dictionary<string, object> metadata)
    {
        var activity = _activitySource.StartActivity(TelemetryConstants.Activities.AppendToStream);

        if (activity == null)
        {
            return activity;
        }

        activity.AddTag(TelemetryConstants.Streams.Name, streamName);

        _propagator.Inject(
            new PropagationContext(activity.Context, default),
            metadata,
            (meta, key, value) => meta[key] = value
        );

        var causationId = activity.Parent?.GetBaggageItem(TelemetryConstants.Message.Id);

        if (causationId != null)
        {
            metadata.Add(MessageConstants.Metadata.CausationId, causationId);
        }

        return activity;
    }

    public Activity? StartSessionAppendToStreamActivity(string streamName, Dictionary<string, object> metadata)
    {
        var activity = _activitySource.StartActivity(TelemetryConstants.Activities.SessionAppendToStream);

        if (activity == null)
        {
            return activity;
        }

        activity.AddTag(TelemetryConstants.Streams.Name, streamName);

        _propagator.Inject(
            new PropagationContext(activity.Context, default),
            metadata,
            (meta, key, value) => meta[key] = value
        );

        var causationId = activity.Parent?.GetBaggageItem(TelemetryConstants.Message.Id);

        if (causationId != null)
        {
            metadata.Add(MessageConstants.Metadata.CausationId, causationId);
        }

        return activity;
    }

    public Activity? StartReadStreamActivity(string streamName)
    {
        var activity = _activitySource.StartActivity(TelemetryConstants.Activities.ReadStream);

        activity?.AddTag(TelemetryConstants.Streams.Name, streamName);

        return activity;
    }

    public Activity? StartReadStreamBatchActivity() =>
        _activitySource.StartActivity(TelemetryConstants.Activities.ReadStreamBatch);

    public Activity? StartScheduleMessageActivity(string streamName, Dictionary<string, object> metadata)
    {
        var activity = _activitySource.StartActivity(TelemetryConstants.Activities.ScheduleMessage);

        if (activity == null)
        {
            return activity;
        }

        activity.AddTag(TelemetryConstants.Streams.Name, streamName);

        _propagator.Inject(
            new PropagationContext(activity.Context, default),
            metadata,
            (meta, key, value) => meta[key] = value
        );

        var causationId = activity.Parent?.GetBaggageItem(TelemetryConstants.Message.Id);

        if (causationId != null)
        {
            metadata.Add(MessageConstants.Metadata.CausationId, causationId);
        }

        return activity;
    }

    public Activity? StartSessionSaveChangesActivity() =>
        _activitySource.StartActivity(TelemetryConstants.Activities.SessionSaveChanges);

    public Activity? StartHandleMessageActivity(Subscription subscription, IMessageContext messageContext)
    {
        var parentContext = _propagator.Extract(
            default,
            messageContext.Metadata,
            ExtractTraceContextFromMetadata
        );

        var activity = _activitySource.StartActivity(
            $"{_options.Subscriptions.GroupName}.{subscription.Name}",
            ActivityKind.Internal,
            parentContext.ActivityContext
        );

        if (activity == null)
        {
            return activity;
        }

        activity.AddTag(TelemetryConstants.Subscription.GroupName, _options.Subscriptions.GroupName);
        activity.AddTag(TelemetryConstants.Subscription.Name, subscription.Name);
        activity.AddTag(TelemetryConstants.Subscription.Category, subscription.Category);
        activity.AddTag(TelemetryConstants.Subscription.Handler, subscription.HandlerName);
        activity.AddTag(TelemetryConstants.Message.Id, messageContext.Id);

        if (messageContext.Metadata.RootElement.TryGetProperty(
                MessageConstants.Metadata.CausationId,
                out var causationIdProperty
            ))
        {
            activity.AddTag(TelemetryConstants.Message.CausationId, causationIdProperty.GetString());
        }

        activity.AddTag(TelemetryConstants.Message.StreamName, messageContext.StreamName);
        activity.AddTag(TelemetryConstants.Message.GlobalPosition, messageContext.GlobalPosition);
        activity.AddTag(TelemetryConstants.Message.StreamPosition, messageContext.StreamPosition);
        activity.AddTag(TelemetryConstants.Message.Type, messageContext.Type);

        activity.AddBaggage(TelemetryConstants.Message.Id, messageContext.Id);

        return activity;
    }

    private long GetSubscriptionLag()
    {
        using var connection = _database.CreateConnection();

        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = $@"
            WITH lagging AS (
                SELECT COUNT(*) AS lagging
                FROM {_options.Postgres.Schema}.subscriptions s
                INNER JOIN {_options.Postgres.Schema}.checkpoints c ON s.group_name = c.group_name AND s.name = c.name
                WHERE s.status = 'active'
                AND c.status = 'lagging'
                GROUP BY c.group_name, c.name
            )
            SELECT COUNT(*)
            FROM lagging l;
        ";

        command.Prepare();

        return (long)command.ExecuteScalar()!;
    }

    private long GetSubscriptionRetryCount()
    {
        using var connection = _database.CreateConnection();

        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = $@"
            SELECT count(*)
            FROM {_options.Postgres.Schema}.checkpoints
            WHERE status = 'retry';
        ";

        command.Prepare();

        return (long)command.ExecuteScalar()!;
    }

    private long GetSubscriptionFailedCount()
    {
        using var connection = _database.CreateConnection();

        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = $@"
            SELECT count(*)
            FROM {_options.Postgres.Schema}.checkpoints
            WHERE status = 'failed';
        ";

        command.Prepare();

        return (long)command.ExecuteScalar()!;
    }

    private IEnumerable<string> ExtractTraceContextFromMetadata(JsonDocument metadata, string key)
    {
        try
        {
            if (metadata.RootElement.TryGetProperty(key, out var value))
            {
                return [value.GetRawText()];
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unable to extract trace context from metadata");
        }

        return [];
    }
}
