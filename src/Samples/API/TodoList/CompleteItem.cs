using Beckett;
using TodoList.CompleteItem;

namespace API.TodoList;

public static class CompleteItem
{
    public static RouteGroupBuilder CompleteItemRoute(this RouteGroupBuilder builder)
    {
        builder.MapPost("/{id:guid}/complete/{item}", Handler);

        return builder;
    }

    private static async Task<IResult> Handler(
        Guid id,
        string item,
        IMessageStore messageStore,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var command = new CompleteTodoListItem(id, item);

            //TODO - add polly retry example here
            var result = await command.Execute(messageStore, cancellationToken);

            return Results.Ok(
                new
                {
                    id,
                    item,
                    result.StreamVersion
                }
            );
        }
        catch (ItemAlreadyCompletedException)
        {
            return Results.Conflict();
        }
    }
}
