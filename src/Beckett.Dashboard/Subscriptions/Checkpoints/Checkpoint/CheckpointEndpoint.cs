using Beckett.Database;

namespace Beckett.Dashboard.Subscriptions.Checkpoints.Checkpoint;

public static class CheckpointEndpoint
{
    public static async Task<IResult> Handle(
        long id,
        IPostgresDatabase database,
        PostgresOptions options,
        CancellationToken cancellationToken
    )
    {
        var result = await database.Execute(new CheckpointQuery(id), cancellationToken);

        return result == null
            ? Results.NotFound()
            : Results.Extensions.Render<Checkpoint>(new Checkpoint.ViewModel(result));
    }
}
