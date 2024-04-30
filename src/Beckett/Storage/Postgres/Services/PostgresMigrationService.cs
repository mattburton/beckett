using Microsoft.Extensions.Hosting;

namespace Beckett.Storage.Postgres.Services;

public class PostgresMigrationService(BeckettOptions options) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return PostgresMigrator.Execute(
            options.Postgres.MigrationConnectionString,
            options.Postgres.Schema,
            options.Postgres.MigrationAdvisoryLockId,
            cancellationToken
        );
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
