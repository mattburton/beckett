using TodoList.CompleteItem;

namespace API.TodoLists;

public static class CompleteItem
{
    public static void Route(RouteGroupBuilder builder)
    {
        builder.MapPost("/{id:guid}/complete/{item}", Handler);
    }

    private static async Task<Results<Ok<CompleteItemResponse>, Conflict>> Handler(
        Guid id,
        string item,
        IMessageStore messageStore,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var command = new CompleteItemCommand(id, item);

            var result = await command.Execute(messageStore, cancellationToken);

            return TypedResults.Ok(new CompleteItemResponse(id, item, result.StreamVersion));
        }
        catch (ItemAlreadyCompletedException)
        {
            return TypedResults.Conflict();
        }
    }
}

[UsedImplicitly]
public record CompleteItemResponse(Guid Id, string Item, long StreamVersion);
