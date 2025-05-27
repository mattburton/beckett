using TodoList.GetList;

namespace API.TodoLists;

public static class GetList
{
    public static void Route(RouteGroupBuilder builder)
    {
        builder.MapGet("/{id:guid}", Handler);
    }

    private static async Task<Results<Ok<GetListResponse>, NotFound>> Handler(
        Guid id,
        IMessageStore messageStore,
        CancellationToken cancellationToken
    )
    {
        var query = new GetListQuery(id);

        var model = await query.Execute(messageStore, cancellationToken);

        if (model == null)
        {
            return TypedResults.NotFound();
        }

        var response = new GetListResponse(model.Id, model.Name, model.Items);

        return TypedResults.Ok(response);
    }
}

[UsedImplicitly]
public record GetListResponse(Guid Id, string Name, Dictionary<string, bool> Items);
