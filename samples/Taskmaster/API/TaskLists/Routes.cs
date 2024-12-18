namespace API.TaskLists;

public static class Routes
{
    public static RouteGroupBuilder TaskListRoutes(this RouteGroupBuilder builder)
    {
        builder.MapPost("/", CreateList.Handler);
        builder.MapGet("/{id:guid}", GetList.Handler);
        builder.MapPost("/{id:guid}", AddTask.Handler);
        builder.MapPost("/{id:guid}/complete/{item}", CompleteTask.Handler);

        return builder;
    }
}
