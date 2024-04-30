using MinimalApi.TodoList.AddingItems;
using MinimalApi.TodoList.CreatingLists;
using MinimalApi.TodoList.GetTodoList;

namespace MinimalApi.TodoList;

public static class Routes
{
    public static RouteGroupBuilder UseTodoListRoutes(this RouteGroupBuilder builder) =>
        builder
            .AddTodoListItemRoute()
            .CreateTodoListRoute()
            .GetTodoListRoute()
            .WithTags("Todo List API");
}
