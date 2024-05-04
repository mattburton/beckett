using MinimalApi.TodoList.CreatingLists;

namespace MinimalApi.TodoList.NotifyWhenListCreated;

public static class Configuration
{
    public static IBeckettBuilder UseNotifyWhenListCreated(this IBeckettBuilder builder)
    {
        builder.Services.AddScoped<TodoListCreatedHandler>();

        builder.AddSubscription<TodoListCreatedHandler, TodoListCreated>(
            nameof(TodoListCreatedHandler),
            (handler, @event, token) => handler.Handle(@event, token)
        );

        return builder;
    }
}
