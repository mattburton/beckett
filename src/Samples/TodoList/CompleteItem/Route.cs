namespace TodoList.CompleteItem;

public static class Route
{
    public static RouteGroupBuilder CompleteTodoListItemRoute(this RouteGroupBuilder builder)
    {
        builder.MapPost(
            "/{id}/complete/{item}",
            async (
                Guid id,
                string item,
                IMessageStore messageStore,
                CancellationToken cancellationToken
            ) =>
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
        ).WithName("Complete Todo List Item").WithOpenApi();

        return builder;
    }
}
