namespace API.V1.TaskLists;

public static class Routes
{
    public static RouteGroupBuilder MapTaskListRoutes(this RouteGroupBuilder builder)
    {
        var routes = builder.MapGroup("task-lists");

        routes.MapPost("/", AddTaskListEndpoint.Handle);
        routes.MapGet("/", GetTaskListsEndpoint.Handle);
        routes.MapGet("/{taskListId:guid}", GetTaskListEndpoint.Handle);
        routes.MapPut("/{taskListId:guid}/name", ChangeTaskListNameEndpoint.Handle);
        routes.MapDelete("/{taskListId:guid}", DeleteTaskListEndpoint.Handle);
        routes.MapPost("/{taskListId:guid}", AddTaskEndpoint.Handle);
        routes.MapPost("/{taskListId:guid}/complete/{task}", CompleteTaskEndpoint.Handle);

        return builder;
    }
}
