using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Dashboard.Postgres.Subscriptions.Queries;

public class GetReservations(
    string? query,
    int offset,
    int limit,
    PostgresOptions options
) : IPostgresDatabaseQuery<GetReservationsResult>
{
    public async Task<GetReservationsResult> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        command.CommandText = $@"
            SELECT id, group_name, name, stream_name, stream_position, reserved_until, count(*) over() as total_results
            FROM {options.Schema}.checkpoints
            WHERE reserved_until IS NOT NULL
            AND ($1 is null or (group_name ILIKE '%' || $1 || '%' OR name ILIKE '%' || $1 || '%' OR stream_name ILIKE '%' || $1 || '%'))
            ORDER BY reserved_until
            OFFSET $2
            LIMIT $3;
        ";

        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text, IsNullable = true });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });

        if (options.PrepareStatements)
        {
            await command.PrepareAsync(cancellationToken);
        }

        command.Parameters[0].Value = string.IsNullOrWhiteSpace(query) ? DBNull.Value : query;
        command.Parameters[1].Value = offset;
        command.Parameters[2].Value = limit;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<GetReservationsResult.Reservation>();

        int? totalResults = null;

        while (await reader.ReadAsync(cancellationToken))
        {
            totalResults ??= reader.GetFieldValue<int>(6);

            results.Add(
                new GetReservationsResult.Reservation(
                    reader.GetFieldValue<long>(0),
                    reader.GetFieldValue<string>(1),
                    reader.GetFieldValue<string>(2),
                    reader.GetFieldValue<string>(3),
                    reader.GetFieldValue<long>(4),
                    reader.GetFieldValue<DateTimeOffset>(5)
                )
            );
        }

        return new GetReservationsResult(results, totalResults.GetValueOrDefault(0));
    }
}
