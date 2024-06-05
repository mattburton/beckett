using Beckett.Database.Queries;

namespace Beckett.Dashboard.Components;

public static class RetriesComponent
{
    public static RouteGroupBuilder RetriesRoute(this RouteGroupBuilder builder)
    {
        builder.MapGet("/components/retries", Handler);

        return builder;
    }

    public static async Task<IResult> Handler(
        IPostgresDatabase database,
        BeckettOptions options,
        CancellationToken cancellationToken
    )
    {
        var results = await database.Execute(new GetSubscriptionRetryCount(options.ApplicationName), cancellationToken);

        return new Retries(new ViewModel(results));
    }

    public record ViewModel(long Value);
}
