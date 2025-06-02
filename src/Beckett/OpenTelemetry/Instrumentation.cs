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

    public Activity? StartAppendToStreamActivity(string streamName, Dictionary<string, string> metadata)
    {
        var activity = _activitySource.StartActivity(TelemetryConstants.Activities.AppendToStream);

        activity?.AddTag(TelemetryConstants.Streams.Name, streamName);

        if (activity is not null)
        {
            _propagator.Inject(
                new PropagationContext(activity.Context, default),
                metadata,
                (meta, key, value) => meta[key] = value
            );
        }

        var correlationId = BeckettContext.GetCorrelationId();

        if (correlationId != null)
        {
            metadata.Add(MessageConstants.Metadata.CorrelationId, correlationId);
        }

        var causationId = activity?.Parent?.GetBaggageItem(TelemetryConstants.Message.Id);

        if (causationId != null)
        {
            metadata.Add(MessageConstants.Metadata.CausationId, causationId);
        }

        metadata.Add(MessageConstants.Metadata.Tenant, BeckettContext.GetTenant());

        return activity;
    }

    public Activity? StartReadStreamActivity(string streamName)
    {
        var activity = _activitySource.StartActivity(TelemetryConstants.Activities.ReadStream);

        activity?.AddTag(TelemetryConstants.Streams.Name, streamName);

        return activity;
    }

    public Activity? StartScheduleMessageActivity(string streamName, Dictionary<string, string> metadata)
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

        var correlationId = BeckettContext.GetCorrelationId();

        if (correlationId != null)
        {
            metadata.Add(MessageConstants.Metadata.CorrelationId, correlationId);
        }

        var causationId = activity.Parent?.GetBaggageItem(TelemetryConstants.Message.Id);

        if (causationId != null)
        {
            metadata.Add(MessageConstants.Metadata.CausationId, causationId);
        }

        metadata.Add(MessageConstants.Metadata.Tenant, BeckettContext.GetTenant());

        return activity;
    }

    public Activity? StartHandleMessageActivity(Subscription subscription, IMessageContext messageContext)
    {
        var parentContext = _propagator.Extract(
            default,
            messageContext.Metadata,
            ExtractTraceContextFromMetadata
        );

        var activity = _activitySource.StartActivity(
            subscription.Name,
            ActivityKind.Internal,
            parentContext.ActivityContext
        );

        if (activity == null)
        {
            return activity;
        }

        activity.AddTag(TelemetryConstants.Subscription.GroupName, subscription.Group.Name);
        activity.AddTag(TelemetryConstants.Subscription.Name, subscription.Name);

        if (!string.IsNullOrWhiteSpace(subscription.Category))
        {
            activity.AddTag(TelemetryConstants.Subscription.Category, subscription.Category);
        }

        if (!string.IsNullOrWhiteSpace(subscription.HandlerName))
        {
            activity.AddTag(TelemetryConstants.Subscription.Handler, subscription.HandlerName);
        }

        activity.AddTag(TelemetryConstants.Message.Id, messageContext.Id);

        if (messageContext.Metadata.TryGetProperty(
                MessageConstants.Metadata.CorrelationId,
                out var correlationIdProperty
            ))
        {
            var correlationId = correlationIdProperty.GetString();

            activity.AddTag(TelemetryConstants.Message.CorrelationId, correlationId);
            activity.AddBaggage(TelemetryConstants.Message.CorrelationId, correlationId);
        }

        if (messageContext.Metadata.TryGetProperty(
                MessageConstants.Metadata.CausationId,
                out var causationIdProperty
            ))
        {
            var causationId = causationIdProperty.GetString();

            activity.AddTag(TelemetryConstants.Message.CausationId, causationId);
        }

        if (messageContext.Metadata.TryGetProperty(
                MessageConstants.Metadata.Tenant,
                out var tenantProperty
            ))
        {
            var tenant = tenantProperty.GetString();

            activity.AddTag(TelemetryConstants.Message.Tenant, tenant);
            activity.AddBaggage(TelemetryConstants.Message.Tenant, tenant);
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

    private IEnumerable<string> ExtractTraceContextFromMetadata(JsonElement metadata, string key)
    {
        try
        {
            if (metadata.TryGetProperty(key, out var value))
            {
                if (value.ValueKind == JsonValueKind.Null)
                {
                    return [];
                }

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
