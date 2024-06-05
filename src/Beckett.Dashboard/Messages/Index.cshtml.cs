namespace Beckett.Dashboard.Messages;

public static class IndexPage
{
    public static RouteGroupBuilder IndexRoute(this RouteGroupBuilder builder)
    {
        builder.MapGet("/messages/{id:guid}", Handler);

        return builder;
    }

    public static async Task<IResult> Handler(
        Guid id,
        IPostgresDatabase database,
        CancellationToken cancellationToken
    )
    {
        var result = await database.Execute(new GetMessage(id), cancellationToken);

        if (result is null)
        {
            return Results.NotFound();
        }

        var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(result.Metadata) ??
                       new Dictionary<string, object>();

        return new Index(new ViewModel(result.Category, result.StreamName, id, result.Type, result.Data, metadata));
    }

    public record ViewModel(
        string Category,
        string StreamName,
        Guid Id,
        string Type,
        string Data,
        Dictionary<string, object> Metadata
    );
}
