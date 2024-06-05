using Beckett;
using TodoList.AddItem;

namespace API.TodoList;

public static class AddItem
{
    public static RouteGroupBuilder AddItemRoute(this RouteGroupBuilder builder)
    {
        builder.MapPost("/{id:guid}", Handler).WithName("Add Todo List Item").WithOpenApi();

        return builder;
    }

    private static async Task<IResult> Handler(
        Guid id,
        Request request,
        IMessageStore messageStore,
        CancellationToken cancellationToken
    )
    {
        try
        {
            //TODO - add polly retry example here
            var result = await new AddTodoListItem(id, request.Item).Execute(messageStore, cancellationToken);

            return Results.Ok(
                new
                {
                    id,
                    request.Item,
                    result.StreamVersion
                }
            );
        }
        catch (ItemAlreadyAddedException)
        {
            return Results.Conflict();
        }
    }

    public record Request(string Item);
}
