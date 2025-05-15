using Npgsql;
using Polly;
using Polly.Retry;

namespace Beckett.Database;

public class PostgresDatabase(IPostgresDataSource dataSource) : IPostgresDatabase
{
    public async Task<T> Execute<T>(IPostgresDatabaseQuery<T> query, CancellationToken cancellationToken)
    {
        await using var connection = dataSource.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        return await Pipeline.ExecuteAsync(
            static async (state, token) =>
            {
                await using var command = state.connection.CreateCommand();

                return await state.query.Execute(command, token);
            },
            (connection, query),
            cancellationToken
        );
    }

    public async Task<T> Execute<T>(
        IPostgresDatabaseQuery<T> query,
        NpgsqlConnection connection,
        CancellationToken cancellationToken
    )
    {
        return await Pipeline.ExecuteAsync(
            static async (state, token) =>
            {
                await using var command = state.connection.CreateCommand();

                return await state.query.Execute(command, token);
            },
            (connection, query),
            cancellationToken
        );
    }

    public async Task<T> Execute<T>(
        IPostgresDatabaseQuery<T> query,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken
    )
    {
        return await Pipeline.ExecuteAsync(
            static async (state, token) =>
            {
                await using var command = state.connection.CreateCommand();

                command.Transaction = state.transaction;

                return await state.query.Execute(command, token);
            },
            (connection, transaction, query),
            cancellationToken
        );
    }

    private static readonly ResiliencePipeline Pipeline = new ResiliencePipelineBuilder().AddRetry(
        new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<NpgsqlException>(e => e.IsTransient),
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(50),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true
        }
    ).Build();
}
