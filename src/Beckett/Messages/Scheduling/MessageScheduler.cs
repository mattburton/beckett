using Beckett.Database;
using Beckett.Database.Queries;
using Beckett.Database.Types;

namespace Beckett.Messages.Scheduling;

public class MessageScheduler(
    IPostgresDatabase database,
    IMessageSerializer messageSerializer
) : IMessageScheduler
{
    public async Task ScheduleMessages(
        string streamName,
        IEnumerable<ScheduledMessageEnvelope> messages,
        CancellationToken cancellationToken
    )
    {
        var scheduledMessages = messages.Select(x => ScheduledMessageType.From(
            x.Message,
            x.Metadata,
            x.DeliverAt,
            messageSerializer
        )).ToArray();

        await database.Execute(new ScheduleMessages(streamName, scheduledMessages), cancellationToken);
    }
}
