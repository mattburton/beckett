using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Beckett.Database;
using Beckett.Messages;
using Beckett.Subscriptions;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using OpenTelemetry.Context.Propagation;

namespace Beckett.OpenTelemetry;

[SuppressMessage("ReSharper", "ExplicitCallerInfoArgument")]
public class Instrumentation : IInstrumentation, IDisposable
{
    private readonly ActivitySource _activitySource;
    private readonly IPostgresDatabase _database;
    private readonly ILogger<Instrumentation> _logger;
    private readonly IMessageTypeMap _messageTypeMap;
    private readonly Meter? _meter;
    private readonly BeckettOptions _options;
    private readonly TextMapPropagator _propagator;

    public Instrumentation(
        IPostgresDatabase database,
        IMeterFactory meterFactory,
        IMessageTypeMap messageTypeMap,
        BeckettOptions options,
        ILogger<Instrumentation> logger
    )
    {
        _database = database;
        _messageTypeMap = messageTypeMap;
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

    public Activity? StartReadStreamActivity(string streamName)
    {
        var activity = _activitySource.StartActivity(TelemetryConstants.Activities.ReadStream);

        activity?.AddTag(TelemetryConstants.Streams.Name, streamName);

        return activity;
    }

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

    public Activity? StartHandleMessageActivity(Subscription subscription, IMessageContext messageContext)
    {
        var parentContext = _propagator.Extract(
            default,
            messageContext.Metadata,
            ExtractTraceContextFromMetadata
        );

        var activity = _activitySource.StartActivity(
            TelemetryConstants.Activities.HandleMessage,
            ActivityKind.Internal,
            parentContext.ActivityContext
        );

        if (activity == null)
        {
            return activity;
        }

        activity.AddTag(TelemetryConstants.Application.Name, _options.ApplicationName);
        activity.AddTag(TelemetryConstants.Subscription.Name, subscription.Name);
        activity.AddTag(TelemetryConstants.Subscription.Category, subscription.Category);
        activity.AddTag(TelemetryConstants.Subscription.Handler, subscription.HandlerName);
        activity.AddTag(TelemetryConstants.Message.Id, messageContext.Id);

        if (messageContext.Metadata.TryGetValue(MessageConstants.Metadata.CausationId, out var causationId))
        {
            activity.AddTag(TelemetryConstants.Message.CausationId, causationId);
        }

        activity.AddTag(TelemetryConstants.Message.StreamName, messageContext.StreamName);
        activity.AddTag(TelemetryConstants.Message.GlobalPosition, messageContext.GlobalPosition);
        activity.AddTag(TelemetryConstants.Message.StreamPosition, messageContext.StreamPosition);
        activity.AddTag(TelemetryConstants.Message.Type, _messageTypeMap.GetName(messageContext.Type));

        activity.AddBaggage(TelemetryConstants.Message.Id, messageContext.Id.ToString());

        return activity;
    }

    private long GetSubscriptionLag()
    {
        using var connection = _database.CreateConnection();

        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = $"select {_options.Postgres.Schema}.get_subscription_lag($1);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        command.Prepare();

        command.Parameters[0].Value = _options.ApplicationName;

        return (long)command.ExecuteScalar()!;
    }

    private long GetSubscriptionRetryCount()
    {
        using var connection = _database.CreateConnection();

        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = $"select {_options.Postgres.Schema}.get_subscription_retry_count($1);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        command.Prepare();

        command.Parameters[0].Value = _options.ApplicationName;

        return (long)command.ExecuteScalar()!;
    }

    private long GetSubscriptionFailedCount()
    {
        using var connection = _database.CreateConnection();

        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = $"select {_options.Postgres.Schema}.get_subscription_failed_count($1);";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });

        command.Prepare();

        command.Parameters[0].Value = _options.ApplicationName;

        return (long)command.ExecuteScalar()!;
    }

    private IEnumerable<string> ExtractTraceContextFromMetadata(IDictionary<string, object> metadata, string key)
    {
        try
        {
            if (metadata.TryGetValue(key, out var value))
            {
                return [value.ToString()!];
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unable to extract trace context from metadata");
        }

        return [];
    }
}
