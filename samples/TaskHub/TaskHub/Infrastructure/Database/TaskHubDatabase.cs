namespace TaskHub.Infrastructure.Database;

public static class TaskHubDatabase
{
    public static async Task Migrate(string connectionString)
    {
        var dataSource = new NpgsqlDataSourceBuilder(connectionString).Build();

        await using var createRole = dataSource.CreateCommand("CREATE ROLE taskhub WITH LOGIN PASSWORD 'password';");

        try
        {
            await createRole.ExecuteNonQueryAsync();
        }
        catch (PostgresException e) when (e.SqlState == "42710")
        {
            // ignore - role already exists
        }

        await using var assignRole = dataSource.CreateCommand("GRANT beckett TO taskhub;");

        await assignRole.ExecuteNonQueryAsync();

        await using var taskmasterSchema = dataSource.CreateCommand(@"
            CREATE SCHEMA IF NOT EXISTS taskhub;
            GRANT USAGE ON SCHEMA taskhub to taskhub;
            GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA taskhub TO taskhub;
        ");

        await taskmasterSchema.ExecuteNonQueryAsync();

        await using var taskListsTable = dataSource.CreateCommand(@"
            CREATE TABLE IF NOT EXISTS taskhub.task_list_view (
                id uuid PRIMARY KEY,
                name text
            );
        ");

        await taskListsTable.ExecuteNonQueryAsync();
    }
}
