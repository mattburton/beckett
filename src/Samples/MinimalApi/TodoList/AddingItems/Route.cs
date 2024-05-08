namespace MinimalApi.TodoList.AddingItems;

public static class Route
{
    public static RouteGroupBuilder AddTodoListItemRoute(this RouteGroupBuilder builder)
    {
        builder.MapPost("/{id}", async (
            AddTodoListItem command,
            IMessageStore messageStore,
            CancellationToken cancellationToken
        ) =>
        {
            try
            {
                //TODO - add polly retry example here
                var result = await command.Execute(messageStore, cancellationToken);

                return Results.Ok(new
                {
                    command.Id,
                    command.Item,
                    result.StreamVersion
                });
            }
            catch (ItemAlreadyAddedException)
            {
                return Results.Conflict();
            }
        }).WithName("Add Todo List Item").WithOpenApi();

        return builder;
    }
}
