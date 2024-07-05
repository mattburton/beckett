namespace Beckett.Dashboard.Subscriptions;

public static class FailedPage
{
    public static RouteGroupBuilder FailedPageRoute(this RouteGroupBuilder builder)
    {
        builder.MapGet("/subscriptions/failed", Handler);

        return builder;
    }

    public static async Task<IResult> Handler(IDashboard dashboard, CancellationToken cancellationToken)
    {
        var result = await dashboard.Subscriptions.GetFailed(cancellationToken);

        return new Failed(new ViewModel(result.Failures));
    }

    public record ViewModel(List<GetFailedResult.Failure> Failures);
}
