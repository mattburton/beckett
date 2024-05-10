using TodoList.AddItem;
using TodoList.CompleteItem;
using TodoList.CreateList;
using TodoList.GetList;

namespace TodoList;

public static class Routes
{
    public static RouteGroupBuilder UseTodoListRoutes(this RouteGroupBuilder builder) =>
        builder
            .AddTodoListItemRoute()
            .CompleteTodoListItemRoute()
            .CreateTodoListRoute()
            .GetTodoListRoute()
            .WithTags("Todo List API");
}
