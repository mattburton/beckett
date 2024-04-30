using Microsoft.Extensions.Hosting;

namespace Beckett.Database.Services;

public class MigrationService(BeckettOptions options) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Migrator.Execute(options.Database, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
