namespace Beckett.Dashboard.Subscriptions;

public static class CheckpointPage
{
    public static RouteGroupBuilder CheckpointPageRoute(this RouteGroupBuilder builder)
    {
        builder.MapGet("/subscriptions/checkpoints/{id:long}", Handler);

        return builder;
    }

    public static async Task<IResult> Handler(long id, IDashboard dashboard, CancellationToken cancellationToken)
    {
        var result = await dashboard.Subscriptions.GetCheckpoint(id, cancellationToken);

        return result == null ? Results.NotFound() : new Checkpoint(new ViewModel(result));
    }

    public record ViewModel(GetCheckpointResult Details);
}
