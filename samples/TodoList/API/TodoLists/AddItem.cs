using TodoList.AddItem;

namespace API.TodoLists;

public static class AddItem
{
    public static void Route(RouteGroupBuilder builder)
    {
        builder.MapPost("/{id:guid}", Handler);
    }

    private static async Task<Results<Ok<AddItemResponse>, Conflict>> Handler(
        Guid id,
        AddItemRequest request,
        IMessageStore messageStore,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var command = new AddItemCommand(id, request.Item);

            var result = await command.Execute(messageStore, cancellationToken);

            return TypedResults.Ok(new AddItemResponse(id, request.Item, result.StreamVersion));
        }
        catch (ItemAlreadyAddedException)
        {
            return TypedResults.Conflict();
        }
    }
}

[UsedImplicitly]
public record AddItemRequest(string Item);

[UsedImplicitly]
public record AddItemResponse(Guid Id, string Item, long StreamVersion);
