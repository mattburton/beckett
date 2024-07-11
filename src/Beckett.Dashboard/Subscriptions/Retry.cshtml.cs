namespace Beckett.Dashboard.Subscriptions;

public static class RetryPage
{
    public static RouteGroupBuilder RetryPageRoute(this RouteGroupBuilder builder)
    {
        builder.MapGet("/subscriptions/retries/{id:guid}", Handler);

        return builder;
    }

    public static async Task<IResult> Handler(Guid id, IDashboard dashboard, CancellationToken cancellationToken)
    {
        var result = await dashboard.Subscriptions.GetRetryDetails(id, cancellationToken);

        return result == null ? Results.NotFound() : new Retry(new ViewModel(result));
    }

    public record ViewModel(GetRetryDetailsResult Details);
}
