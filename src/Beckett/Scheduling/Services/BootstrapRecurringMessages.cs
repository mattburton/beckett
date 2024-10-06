using Beckett.Database;
using Microsoft.Extensions.Hosting;

namespace Beckett.Scheduling.Services;

public class BootstrapRecurringMessages(
    IPostgresDatabase database,
    IMessageScheduler messageScheduler
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var recurringMessage in RecurringMessageRegistry.All())
        {
            await using var connection = database.CreateConnection();

            await connection.OpenAsync(cancellationToken);

            await messageScheduler.RecurringMessage(
                recurringMessage.Name,
                recurringMessage.CronExpression,
                recurringMessage.StreamName,
                recurringMessage.Message,
                cancellationToken
            );
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
