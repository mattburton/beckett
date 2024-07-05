namespace Beckett.Dashboard.Subscriptions;

public static class RetriesPage
{
    public static RouteGroupBuilder RetriesPageRoute(this RouteGroupBuilder builder)
    {
        builder.MapGet("/subscriptions/retries", Handler);

        return builder;
    }

    public static async Task<IResult> Handler(IDashboard dashboard, CancellationToken cancellationToken)
    {
        var result = await dashboard.Subscriptions.GetRetries(cancellationToken);

        return new Retries(new ViewModel(result.Retries));
    }

    public record ViewModel(List<GetRetriesResult.Retry> Retries);
}
