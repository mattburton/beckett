namespace Beckett.Dashboard.Subscriptions;

public static class RetryPage
{
    public static RouteGroupBuilder RetryPageRoute(this RouteGroupBuilder builder)
    {
        builder.MapGet("/subscriptions/retries/{id:long}", Handler);

        return builder;
    }

    public static async Task<IResult> Handler(long id, IDashboard dashboard, CancellationToken cancellationToken)
    {
        var result = await dashboard.Subscriptions.GetRetryDetails(id, cancellationToken);

        return result == null ? Results.NotFound() : new Retry(new ViewModel(result));
    }

    public record ViewModel(GetRetryDetailsResult Details);
}
