using MinimalApi.TodoList.AddingItems;
using MinimalApi.TodoList.Notifications.WhenItemAdded;

namespace MinimalApi.TodoList.NotifyWhenItemAdded;

public static class Configuration
{
    public static IBeckettBuilder UseNotifyWhenItemAdded(this IBeckettBuilder builder)
    {
        builder.Services.AddScoped<TodoListItemAddedHandler>();

        builder.AddSubscription<TodoListItemAddedHandler, TodoListItemAdded>(
            nameof(TodoListItemAddedHandler),
            (handler, @event, token) => handler.Handle(@event, token)
        );

        return builder;
    }
}
