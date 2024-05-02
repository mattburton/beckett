namespace MinimalApi.TodoList.CreatingLists;

public static class Route
{
    public static RouteGroupBuilder CreateTodoListRoute(this RouteGroupBuilder builder)
    {
        builder.MapPost("/", async (
            CreateTodoList command,
            IEventStore eventStore,
            CancellationToken cancellationToken
        ) =>
        {
            var result = await command.Execute(eventStore, cancellationToken);

            return Results.Ok(new
            {
                command.Id,
                command.Name,
                result.StreamVersion
            });
        }).WithName("Create Todo List").WithOpenApi();

        return builder;
    }
}