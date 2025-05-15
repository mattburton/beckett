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

        await using var command = connection.CreateCommand();

        return await query.Execute(command, cancellationToken);
    }

    public async Task<T> ExecuteWithRetry<T>(IPostgresDatabaseQuery<T> query, CancellationToken cancellationToken)
    {
        await using var connection = dataSource.CreateConnection();

        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();

        return await Pipeline.ExecuteAsync(
            static async (state, token) => await state.query.Execute(state.command, token),
            (connection, query, command),
            cancellationToken
        );
    }

    public async Task<T> Execute<T>(
        IPostgresDatabaseQuery<T> query,
        NpgsqlConnection connection,
        CancellationToken cancellationToken
    )
    {
        await using var command = connection.CreateCommand();

        return await query.Execute(command, cancellationToken);
    }

    public async Task<T> ExecuteWithRetry<T>(
        IPostgresDatabaseQuery<T> query,
        NpgsqlConnection connection,
        CancellationToken cancellationToken
    )
    {
        await using var command = connection.CreateCommand();

        return await Pipeline.ExecuteAsync(
            static async (state, token) => await state.query.Execute(state.command, token),
            (connection, query, command),
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
        await using var command = connection.CreateCommand();

        command.Transaction = transaction;

        return await query.Execute(command, cancellationToken);
    }

    public async Task<T> ExecuteWithRetry<T>(
        IPostgresDatabaseQuery<T> query,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken
    )
    {
        await using var command = connection.CreateCommand();

        command.Transaction = transaction;

        return await Pipeline.ExecuteAsync(
            static async (state, token) => await state.query.Execute(state.command, token),
            (connection, query, command),
            cancellationToken
        );
    }

    private static readonly ResiliencePipeline Pipeline = new ResiliencePipelineBuilder().AddRetry(
        new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<NpgsqlException>(),
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(50),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true
        }
    ).Build();
}
