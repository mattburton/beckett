namespace Beckett.Dashboard.Categories;

public static class MessagesPage
{
    public static RouteGroupBuilder MessagesRoute(this RouteGroupBuilder builder)
    {
        builder.MapGet("/categories/{category}/{streamName}", Handler);

        return builder;
    }

    public static async Task<IResult> Handler(
        string category,
        string streamName,
        IPostgresDatabase database,
        CancellationToken cancellationToken
    )
    {
        var results = await database.Execute(new GetStreamMessages(streamName), cancellationToken);

        return new Messages(new ViewModel(category, streamName, results));
    }

    public record ViewModel(
        string Category,
        string StreamName,
        IReadOnlyList<GetStreamMessages.Result> Messages);
}
