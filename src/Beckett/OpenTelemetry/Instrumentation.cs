using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Beckett.Database;
using Beckett.Messages;
using Beckett.Subscriptions;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Context.Propagation;

namespace Beckett.OpenTelemetry;

[SuppressMessage("ReSharper", "ExplicitCallerInfoArgument")]
public class Instrumentation : IDisposable, IInstrumentation
{
    private readonly IPostgresDatabase _database;
    private readonly IMessageTypeMap _messageTypeMap;
    private readonly SubscriptionOptions _options;
    private readonly ILogger<Instrumentation> _logger;
    private readonly ActivitySource _activitySource;
    private readonly Meter? _meter;
    private readonly TextMapPropagator _propagator;

    public Instrumentation(
        IPostgresDatabase database,
        IMeterFactory meterFactory,
        IMessageTypeMap messageTypeMap,
        SubscriptionOptions options,
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

        if (!options.Enabled)
        {
            return;
        }

        _meter = meterFactory.Create(TelemetryConstants.ActivitySource.Name, version);

        _meter.CreateObservableGauge(TelemetryConstants.Metrics.SubscriptionLag, GetSubscriptionLag);
    }

    public Activity? StartAppendToStreamActivity(string topic, object streamId, Dictionary<string, object> metadata)
    {
        var activity = _activitySource.StartActivity(TelemetryConstants.Activities.AppendToStream, ActivityKind.Producer);

        activity?.AddTag(TelemetryConstants.Streams.Topic, topic);
        activity?.AddTag(TelemetryConstants.Streams.StreamId, streamId);

        _propagator.Inject(
            new PropagationContext(activity!.Context, default), metadata, (meta, key, value) => meta[key] = value
        );

        var causationId = activity?.Parent?.GetBaggageItem(TelemetryConstants.Message.Id);

        if (causationId != null)
        {
            metadata.Add(MessageConstants.Metadata.CausationId, causationId);
        }

        return activity;
    }

    public Activity? StartReadStreamActivity(string topic, object streamId)
    {
        var activity = _activitySource.StartActivity(TelemetryConstants.Activities.ReadStream, ActivityKind.Producer);

        activity?.AddTag(TelemetryConstants.Streams.Topic, topic);
        activity?.AddTag(TelemetryConstants.Streams.StreamId, streamId);

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
            ActivityKind.Consumer,
            parentContext.ActivityContext
        );

        activity?.AddTag(TelemetryConstants.Application.Name, _options.ApplicationName);
        activity?.AddTag(TelemetryConstants.Subscription.Name, subscription.Name);
        activity?.AddTag(TelemetryConstants.Subscription.Topic, subscription.Topic);
        activity?.AddTag(TelemetryConstants.Subscription.Handler, subscription.Type?.FullName);
        activity?.AddTag(TelemetryConstants.Message.Id, messageContext.Id);

        if (messageContext.Metadata.TryGetValue(MessageConstants.Metadata.CausationId, out var causationId))
        {
            activity?.AddTag(TelemetryConstants.Message.CausationId, causationId);
        }

        activity?.AddTag(TelemetryConstants.Message.Topic, messageContext.Topic);
        activity?.AddTag(TelemetryConstants.Message.StreamId, messageContext.StreamId);
        activity?.AddTag(TelemetryConstants.Message.GlobalPosition, messageContext.GlobalPosition);
        activity?.AddTag(TelemetryConstants.Message.StreamPosition, messageContext.StreamPosition);
        activity?.AddTag(TelemetryConstants.Message.Type, _messageTypeMap.GetName(messageContext.Type));

        activity?.AddBaggage(TelemetryConstants.Message.Id, messageContext.Id.ToString());

        return activity;
    }

    public void Dispose()
    {
        _activitySource.Dispose();

        _meter?.Dispose();

        GC.SuppressFinalize(this);
    }

    private long GetSubscriptionLag()
    {
        using var connection = _database.CreateConnection();

        connection.Open();

        using var command = connection.CreateCommand();

        command.CommandText = $@"
            select count(*)
            from beckett.checkpoints
            where application = '{_options.ApplicationName}'
            and starts_with(name, '$') = false
            and stream_position < stream_version;
        ";

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
