using Beckett.Database;
using TaskHub.Infrastructure.Database;
using Testcontainers.PostgreSql;
using Testcontainers.Xunit;
using Xunit.Abstractions;

namespace Tests.Infrastructure.Tests;

public class DatabaseFixture(
    IMessageSink messageSink
) : ContainerFixture<PostgreSqlBuilder, PostgreSqlContainer>(messageSink)
{
    public NpgsqlDataSource DataSource { get; private set; } = null!;

    protected override PostgreSqlBuilder Configure(PostgreSqlBuilder builder) =>
        builder.WithImage("postgres:16-alpine");

    protected override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        var connectionString = Container.GetConnectionString();

        await BeckettDatabase.Migrate(connectionString);

        await TaskHubDatabase.Migrate(connectionString);

        DataSource = new NpgsqlDataSourceBuilder(connectionString).AddBeckett().Build();
    }
}
