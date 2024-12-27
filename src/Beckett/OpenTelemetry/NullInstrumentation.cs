using System.Diagnostics;
using Beckett.Subscriptions;

namespace Beckett.OpenTelemetry;

public class NullInstrumentation : IInstrumentation
{
    public Activity? StartAppendToStreamActivity(string streamName, Dictionary<string, string> metadata) => null;

    public Activity? StartHandleMessageActivity(Subscription subscription, IMessageContext messageContext) => null;

    public Activity? StartReadStreamActivity(string streamName) => null;

    public Activity? StartScheduleMessageActivity(string streamName, Dictionary<string, string> metadata) => null;
}
