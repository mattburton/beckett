using Beckett.Messages;
using Core.Contracts;
using Core.Processors;
using Xunit;

namespace Core.Testing;

public static class ProcessorResultExtensions
{
    public static void NotificationPublished(this ProcessorResult result, INotification notification)
    {
        var notificationType = notification.GetType();
        var candidates = result.Notifications.Where(x => x.GetType() == notificationType);

        foreach (var candidate in candidates)
        {
            try
            {
                Assert.Equivalent(notification, candidate, true);

                return;
            }
            catch
            {
                //no-op
            }
        }

        Assert.Fail(
            $"No match found: {notificationType.Name} {MessageSerializer.Serialize(notificationType, notification)}"
        );
    }
}
