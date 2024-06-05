using Beckett.Database.Queries;

namespace Beckett.Dashboard.Components;

public static class LagComponent
{
    public static RouteGroupBuilder LagRoute(this RouteGroupBuilder builder)
    {
        builder.MapGet("/components/lag", Handler);

        return builder;
    }

    public static async Task<IResult> Handler(
        IPostgresDatabase database,
        BeckettOptions options,
        CancellationToken cancellationToken
    )
    {
        var results = await database.Execute(new GetSubscriptionLag(options.ApplicationName), cancellationToken);

        return new Lag(new ViewModel(results));
    }

    public record ViewModel(long Value);
}
