namespace TodoList.CreateList;

public static class Route
{
    public static RouteGroupBuilder CreateTodoListRoute(this RouteGroupBuilder builder)
    {
        builder.MapPost(
            "/",
            async (
                CreateTodoList command,
                IMessageStore messageStore,
                CancellationToken cancellationToken
            ) =>
            {
                var result = await command.Execute(messageStore, cancellationToken);

                return Results.Ok(
                    new
                    {
                        command.Id,
                        command.Name,
                        result.StreamVersion
                    }
                );
            }
        ).WithName("Create Todo List").WithOpenApi();

        return builder;
    }
}
