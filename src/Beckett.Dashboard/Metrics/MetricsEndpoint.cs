using Beckett.Database;

namespace Beckett.Dashboard.Metrics;

public static class MetricsEndpoint
{
    public static async Task<IResult> Handle(
        IPostgresDatabase database,
        PostgresOptions options,
        CancellationToken cancellationToken
    )
    {
        var result = await database.Execute(new MetricsQuery(options), cancellationToken);

        return Results.Extensions.Render<Metrics>(
            new Metrics.ViewModel(result.Lagging, result.Retries, result.Failed, false)
        );
    }
}
