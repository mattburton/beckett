namespace Beckett.Dashboard.Categories;

public static class StreamsPage
{
    public static RouteGroupBuilder StreamsRoute(this RouteGroupBuilder builder)
    {
        builder.MapGet("/categories/{category}", Handler);

        return builder;
    }

    public static async Task<IResult> Handler(
        string category,
        IPostgresDatabase database,
        CancellationToken cancellationToken
    )
    {
        var results = await database.Execute(new GetCategoryStreams(category), cancellationToken);

        return new Streams(new ViewModel(category, results));
    }

    public record ViewModel(string Category, IReadOnlyList<GetCategoryStreams.Result> Streams);
}
