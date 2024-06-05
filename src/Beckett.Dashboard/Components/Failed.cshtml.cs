using Beckett.Database.Queries;

namespace Beckett.Dashboard.Components;

public static class FailedComponent
{
    public static RouteGroupBuilder FailedRoute(this RouteGroupBuilder builder)
    {
        builder.MapGet("/components/failed", Handler);

        return builder;
    }

    public static async Task<IResult> Handler(
        IPostgresDatabase database,
        BeckettOptions options,
        CancellationToken cancellationToken
    )
    {
        var results = await database.Execute(
            new GetSubscriptionFailedCount(options.ApplicationName),
            cancellationToken
        );

        return new Failed(new ViewModel(results));
    }

    public record ViewModel(long Value);
}
