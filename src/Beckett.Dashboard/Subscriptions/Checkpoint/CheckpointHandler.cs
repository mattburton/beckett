namespace Beckett.Dashboard.Subscriptions.Checkpoint;

public static class CheckpointHandler
{
    public static async Task<IResult> Get(long id, IDashboard dashboard, CancellationToken cancellationToken)
    {
        var result = await dashboard.Subscriptions.GetCheckpoint(id, cancellationToken);

        return result == null
            ? Results.NotFound()
            : Results.Extensions.Render<Checkpoint>(new Checkpoint.ViewModel(result));
    }
}
