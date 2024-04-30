namespace MinimalApi.TodoList.GetTodoList;

public static class Route
{
    public static RouteGroupBuilder GetTodoListRoute(this RouteGroupBuilder builder)
    {
        builder.MapGet("/{id}", async (Guid id, IEventStore eventStore, CancellationToken cancellationToken) =>
        {
            var stream = await eventStore.ReadStream(StreamName.For<TodoList>(id), cancellationToken);

            return stream.IsEmpty ? Results.NotFound() : Results.Ok(stream.ProjectTo<TodoList>());
        }).Produces<TodoList>().WithName("Get Todo List").WithOpenApi();

        return builder;
    }
}
