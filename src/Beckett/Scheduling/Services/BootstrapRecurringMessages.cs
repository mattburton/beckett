using Microsoft.Extensions.Hosting;

namespace Beckett.Scheduling.Services;

public class BootstrapRecurringMessages(IRecurringMessageManager recurringMessageManager) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var recurringMessage in RecurringMessageRegistry.All())
        {
            await recurringMessageManager.Create(
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
