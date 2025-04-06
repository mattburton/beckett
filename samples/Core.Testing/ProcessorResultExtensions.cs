using Beckett.Messages;
using Core.Contracts;
using Core.Processors;
using Xunit;

namespace Core.Testing;

public static class ProcessorResultExtensions
{
    public static void ExternalEventPublished(this ProcessorResult result, IExternalEvent externalEvent)
    {
        var externalEventType = externalEvent.GetType();
        var candidates = result.ExternalEvents.Where(x => x.GetType() == externalEventType);

        foreach (var candidate in candidates)
        {
            try
            {
                Assert.Equivalent(externalEvent, candidate, true);

                return;
            }
            catch
            {
                //no-op
            }
        }

        Assert.Fail(
            $"No match found: {externalEventType.Name} {MessageSerializer.Serialize(externalEventType, externalEvent)}"
        );
    }
}
