using System.Diagnostics;
using Beckett.Subscriptions;

namespace Beckett.OpenTelemetry;

public interface IInstrumentation
{
    Activity? StartAppendToStreamActivity(string topic, object streamId, Dictionary<string, object> metadata);
    Activity? StartReadStreamActivity(string topic, object streamId);
    Activity? StartHandleMessageActivity(Subscription subscription, IMessageContext messageContext);
}
