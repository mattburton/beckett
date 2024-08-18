using System.Diagnostics;
using Beckett.Subscriptions;

namespace Beckett.OpenTelemetry;

public interface IInstrumentation
{
    Activity? StartAppendToStreamActivity(string streamName, Dictionary<string, object> metadata);
    Activity? StartSessionAppendToStreamActivity(string streamName, Dictionary<string, object> metadata);
    Activity? StartHandleMessageActivity(Subscription subscription, IMessageContext messageContext);
    Activity? StartReadStreamActivity(string streamName);
    Activity? StartReadStreamBatchActivity();
    Activity? StartScheduleMessageActivity(string streamName, Dictionary<string, object> metadata);
    Activity? StartSessionSaveChangesActivity();
}
