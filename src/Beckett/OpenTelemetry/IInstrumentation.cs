using System.Diagnostics;
using Beckett.Subscriptions;

namespace Beckett.OpenTelemetry;

public interface IInstrumentation
{
    Activity? StartAppendToStreamActivity(string streamName, Dictionary<string, object> metadata);
    Activity? StartHandleMessageActivity(Subscription subscription, IMessageContext messageContext);
    Activity? StartReadStreamActivity(string streamName);
    Activity? StartScheduleMessageActivity(string streamName, Dictionary<string, object> metadata);
}
