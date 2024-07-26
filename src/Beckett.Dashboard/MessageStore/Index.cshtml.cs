namespace Beckett.Dashboard.MessageStore;

public static class IndexPage
{
    public static RouteGroupBuilder IndexRoute(this RouteGroupBuilder builder)
    {
        builder.MapGet("/message-store", Handler);

        return builder;
    }

    public static async Task<IResult> Handler(string? query, IDashboard dashboard, CancellationToken cancellationToken)
    {
        var result = await dashboard.MessageStore.GetCategories(query, cancellationToken);

        return new Index(new ViewModel(result.Categories));
    }

    public record ViewModel(List<GetCategoriesResult.Category> Categories);
}
