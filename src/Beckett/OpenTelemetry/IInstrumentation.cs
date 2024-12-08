using System.Diagnostics;
using Beckett.Subscriptions;

namespace Beckett.OpenTelemetry;

public interface IInstrumentation
{
    Activity? StartAppendToStreamActivity(string streamName, Dictionary<string, string> metadata);
    Activity? StartSessionAppendToStreamActivity(string streamName, Dictionary<string, string> metadata);
    Activity? StartHandleMessageActivity(Subscription subscription, IMessageContext messageContext);
    Activity? StartReadStreamActivity(string streamName);
    Activity? StartReadStreamBatchActivity();
    Activity? StartScheduleMessageActivity(string streamName, Dictionary<string, string> metadata);
    Activity? StartSessionSaveChangesActivity();
}
