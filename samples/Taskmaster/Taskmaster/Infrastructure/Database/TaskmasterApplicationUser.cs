using Npgsql;

namespace Taskmaster.Infrastructure.Database;

public static class TaskmasterApplicationUser
{
    public static async Task EnsureExists(string connectionString)
    {
        var dataSource = new NpgsqlDataSourceBuilder(connectionString).Build();

        await using var createRole = dataSource.CreateCommand("CREATE ROLE taskmaster WITH LOGIN PASSWORD 'password';");

        try
        {
            await createRole.ExecuteNonQueryAsync();
        }
        catch (PostgresException e) when (e.SqlState == "42710")
        {
            // Role already exists
        }

        await using var assignRole = dataSource.CreateCommand("GRANT beckett TO taskmaster;");

        await assignRole.ExecuteNonQueryAsync();
    }
}
