using System.Text;
using Npgsql;

namespace Beckett.Storage.Postgres;

public static class PostgresMigrator
{
    public static async Task Execute(
        string connectionString,
        string schema,
        int advisoryLockId,
        CancellationToken cancellationToken
    )
    {
        await using var connection = new NpgsqlConnection(connectionString);

        await connection.OpenAsync(cancellationToken);

        if (!await connection.TryAdvisoryLock(advisoryLockId, cancellationToken))
        {
            return;
        }

        await EnsureSchemaIsCreated(connection, schema, cancellationToken);

        await EnsureMigrationsTableIsCreated(connection, cancellationToken);

        await ApplyMigrations(connection, cancellationToken);

        await connection.AdvisoryUnlock(advisoryLockId, cancellationToken);
    }

    private static async Task ApplyMigrations(
        NpgsqlConnection connection,
        CancellationToken cancellationToken
    )
    {
        var appliedMigrations = await GetAppliedMigrations(connection, cancellationToken);

        foreach (var migration in LoadMigrations())
        {
            if (appliedMigrations.Contains(migration.Name))
            {
                continue;
            }

            await ApplyMigration(migration.Name, migration.Script, connection, cancellationToken);
        }
    }

    private static async Task ApplyMigration(
        string name,
        string script,
        NpgsqlConnection connection,
        CancellationToken cancellationToken
    )
    {
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using var applyCommand = connection.CreateCommand();

        applyCommand.CommandText = script;

        await applyCommand.ExecuteNonQueryAsync(cancellationToken);

        await using var recordCommand = connection.CreateCommand();

        recordCommand.CommandText = "insert into migrations (name) values ($1);";

        recordCommand.Parameters.Add(new NpgsqlParameter<string> { Value = name });

        await recordCommand.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task<List<string>> GetAppliedMigrations(
        NpgsqlConnection connection,
        CancellationToken cancellationToken
    )
    {
        await using var command = connection.CreateCommand();

        command.CommandText = "select name from migrations;";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var previousMigrations = new List<string>();

        while (await reader.ReadAsync(cancellationToken))
        {
            previousMigrations.Add(reader.GetFieldValue<string>(0));
        }

        return previousMigrations;
    }

    private static async Task EnsureMigrationsTableIsCreated(
        NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();

        command.CommandText = @"
            create table if not exists migrations
            (
              name text not null primary key,
              timestamp timestamp with time zone default now() not null
            );
        ";

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task EnsureSchemaIsCreated(
        NpgsqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();

        command.CommandText = $"create schema if not exists \"{schema}\";";

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static IEnumerable<(string Name, string Script)> LoadMigrations()
    {
        var assembly = typeof(PostgresMigrator).Assembly;

        return assembly.GetManifestResourceNames()
            .Where(x => x.EndsWith(".sql"))
            .Select(x => (Name: x, Script: LoadMigration(assembly.GetManifestResourceStream(x)!)))
            .OrderBy(x => x.Name);
    }

    private static string LoadMigration(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, true);

        return reader.ReadToEnd();
    }
}
