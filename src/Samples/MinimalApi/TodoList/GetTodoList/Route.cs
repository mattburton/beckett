namespace MinimalApi.TodoList.GetTodoList;

public static class Route
{
    public static RouteGroupBuilder GetTodoListRoute(this RouteGroupBuilder builder)
    {
        builder.MapGet("/{id}", async (Guid id, IMessageStore messageStore, CancellationToken cancellationToken) =>
        {
            var stream = await messageStore.ReadStream(StreamName.For<TodoList>(id), cancellationToken);

            return stream.IsEmpty ? Results.NotFound() : Results.Ok(stream.ProjectTo<TodoListView>());
        }).Produces<TodoListView>().WithName("Get Todo List").WithOpenApi();

        return builder;
    }
}
