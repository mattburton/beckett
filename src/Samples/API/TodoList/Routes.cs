namespace API.TodoList;

public static class Routes
{
    public static RouteGroupBuilder TodoListRoutes(this RouteGroupBuilder builder) =>
        builder
            .AddItemRoute()
            .CompleteItemRoute()
            .CreateListRoute()
            .GetListRoute()
            .WithTags("Todo List API");
}
