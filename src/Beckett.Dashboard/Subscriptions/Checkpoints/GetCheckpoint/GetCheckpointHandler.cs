namespace Beckett.Dashboard.Subscriptions.Checkpoints.GetCheckpoint;

public static class GetCheckpointHandler
{
    public static async Task<IResult> Get(long id, IDashboard dashboard, CancellationToken cancellationToken)
    {
        var result = await dashboard.Subscriptions.GetCheckpoint(id, cancellationToken);

        return result == null
            ? Results.NotFound()
            : Results.Extensions.Render<Checkpoint>(new Checkpoint.ViewModel(result));
    }
}
