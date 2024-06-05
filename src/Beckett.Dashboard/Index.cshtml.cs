namespace Beckett.Dashboard;

public static class IndexPage
{
    public static RouteGroupBuilder IndexRoute(this RouteGroupBuilder builder)
    {
        builder.MapGet("/", Handler);

        return builder;
    }

    public static async Task<IResult> Handler(IPostgresDatabase database, CancellationToken cancellationToken)
    {
        var results = await database.Execute(new GetCategories(), cancellationToken);

        return new Index(new ViewModel(results));
    }

    public record ViewModel(IReadOnlyList<GetCategories.Result> Categories);
}
