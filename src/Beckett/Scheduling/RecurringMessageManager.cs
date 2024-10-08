using Beckett.Database;
using Beckett.Scheduling.Queries;
using Cronos;

namespace Beckett.Scheduling;

public class RecurringMessageManager(IPostgresDatabase database, PostgresOptions options) : IRecurringMessageManager
{
    public async Task Create(
        string name,
        string cronExpression,
        string streamName,
        Message message,
        CancellationToken cancellationToken
    )
    {
        if (!CronExpression.TryParse(cronExpression, out var parsedCronExpression))
        {
            throw new InvalidOperationException("Invalid cron expression");
        }

        var nextOccurrence = parsedCronExpression.GetNextOccurrence(DateTimeOffset.UtcNow, TimeZoneInfo.Utc);

        if (nextOccurrence == null)
        {
            throw new InvalidOperationException("Unable to calculate next occurrence for cron expression");
        }

        await database.Execute(
            new AddOrUpdateRecurringMessage(
                name,
                cronExpression,
                streamName,
                message,
                nextOccurrence.Value,
                options
            ),
            cancellationToken
        );
    }
}
