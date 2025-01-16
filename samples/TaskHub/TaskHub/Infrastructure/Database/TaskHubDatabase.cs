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

        await using var schemas = dataSource.CreateCommand(@"
            CREATE SCHEMA IF NOT EXISTS task_lists;
            GRANT USAGE ON SCHEMA task_lists to taskhub;
            GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA task_lists TO taskhub;

            CREATE SCHEMA IF NOT EXISTS users;
            GRANT USAGE ON SCHEMA users to taskhub;
            GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA users TO taskhub;
        ");

        await schemas.ExecuteNonQueryAsync();

        await using var tables = dataSource.CreateCommand(@"
            CREATE TABLE IF NOT EXISTS task_lists.task_lists (
                id uuid PRIMARY KEY,
                name text
            );

            CREATE TABLE IF NOT EXISTS task_lists.user_lookup (
                username text PRIMARY KEY,
                email text
            );

            CREATE TABLE IF NOT EXISTS users.users (
                username text PRIMARY KEY,
                email text
            );
        ");

        await tables.ExecuteNonQueryAsync();
    }
}
