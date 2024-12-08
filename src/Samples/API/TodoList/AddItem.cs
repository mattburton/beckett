using Beckett;
using TodoList.AddItem;

namespace API.TodoList;

public static class AddItem
{
    public static RouteGroupBuilder AddItemRoute(this RouteGroupBuilder builder)
    {
        builder.MapPost("/{id:guid}", Handler);

        return builder;
    }

    private static async Task<IResult> Handler(
        Guid id,
        AddItemRequest request,
        IMessageStore messageStore,
        CancellationToken cancellationToken
    )
    {
        try
        {
            //TODO - add polly retry example here
            var result = await new AddTodoListItem(id, request.Item).Execute(messageStore, cancellationToken);

            return Results.Ok(new AddItemResponse(id, request.Item, result.StreamVersion));
        }
        catch (ItemAlreadyAddedException)
        {
            return Results.Conflict();
        }
    }
}

public record AddItemRequest(string Item);

public record AddItemResponse(Guid Id, string Item, long StreamVersion);
