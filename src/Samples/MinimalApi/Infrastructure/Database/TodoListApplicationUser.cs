using Npgsql;

namespace MinimalApi.Infrastructure.Database;

public static class TodoListApplicationUser
{
    public static async Task EnsureExists(string connectionString)
    {
        var dataSource = new NpgsqlDataSourceBuilder(connectionString).Build();

        await using var createRole = dataSource.CreateCommand("CREATE ROLE todo_list WITH LOGIN PASSWORD 'password';");

        try
        {
            await createRole.ExecuteNonQueryAsync();
        }
        catch (PostgresException e) when (e.SqlState == "42710")
        {
            // Role already exists
        }

        await using var assignRole = dataSource.CreateCommand("GRANT beckett TO todo_list;");

        await assignRole.ExecuteNonQueryAsync();
    }
}
