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
    private readonly IPostgresDataSource _dataSource;
    private readonly ILogger<Instrumentation> _logger;
    private readonly Meter? _meter;
    private readonly BeckettOptions _options;
    private readonly TextMapPropagator _propagator;

    public Instrumentation(
        IPostgresDataSource dataSource,
        IMeterFactory meterFactory,
        BeckettOptions options,
        ILogger<Instrumentation> logger
    )
    {
        _dataSource = dataSource;
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

        var correlationId = activity.Parent?.GetBaggageItem(TelemetryConstants.Message.CorrelationId);

        if (correlationId != null)
        {
            metadata.Add(MessageConstants.Metadata.CorrelationId, correlationId);
        }

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

        var correlationId = activity.Parent?.GetBaggageItem(TelemetryConstants.Message.CorrelationId);

        if (correlationId != null)
        {
            metadata.Add(MessageConstants.Metadata.CorrelationId, correlationId);
        }

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

        var correlationId = activity.Parent?.GetBaggageItem(TelemetryConstants.Message.CorrelationId);

        if (correlationId != null)
        {
            metadata.Add(MessageConstants.Metadata.CorrelationId, correlationId);
        }

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
                MessageConstants.Metadata.CorrelationId,
                out var correlationIdProperty
            ))
        {
            var correlationId = correlationIdProperty.GetString();

            activity.AddTag(TelemetryConstants.Message.CorrelationId, correlationId);
            activity.AddBaggage(TelemetryConstants.Message.CorrelationId, correlationId);
        }

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
        using var connection = _dataSource.CreateConnection();

        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = $"select {_options.Postgres.Schema}.get_subscription_lag_count();";

        command.Prepare();

        return (long)command.ExecuteScalar()!;
    }

    private long GetSubscriptionRetryCount()
    {
        using var connection = _dataSource.CreateConnection();

        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = $"select {_options.Postgres.Schema}.get_subscription_retry_count();";

        command.Prepare();

        return (long)command.ExecuteScalar()!;
    }

    private long GetSubscriptionFailedCount()
    {
        using var connection = _dataSource.CreateConnection();

        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = $"select {_options.Postgres.Schema}.get_subscription_failed_count();";

        command.Prepare();

        return (long)command.ExecuteScalar()!;
    }

    private IEnumerable<string> ExtractTraceContextFromMetadata(JsonDocument metadata, string key)
    {
        try
        {
            if (metadata.RootElement.TryGetProperty(key, out var value))
            {
                return [value.ToString()];
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unable to extract trace context from metadata");
        }

        return [];
    }
}
