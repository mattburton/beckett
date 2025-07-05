using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class RecordStreamData(
    string[] categories,
    DateTimeOffset[] categoryTimestamps,
    string[] tenants
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        //language=sql
        const string sql = """
            WITH insert_categories AS (
                INSERT INTO beckett.categories (name, updated_at)
                SELECT d.name, d.timestamp
                FROM unnest($1, $2) AS d (name, timestamp)
                ON CONFLICT (name) DO UPDATE
                SET updated_at = excluded.updated_at
            )
            INSERT INTO beckett.tenants (tenant)
            SELECT d.tenant
            FROM unnest($3) AS d (tenant)
            ON CONFLICT (tenant) DO NOTHING;
        """;

        command.CommandText = Query.Build(nameof(RecordStreamData), sql, out var prepare);

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Text });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.TimestampTz });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Text });

        if (prepare)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = categories;
        command.Parameters[1].Value = categoryTimestamps;
        command.Parameters[2].Value = tenants;

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
