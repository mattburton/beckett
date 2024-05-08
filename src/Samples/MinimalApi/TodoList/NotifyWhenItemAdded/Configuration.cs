using Beckett.Subscriptions;
using MinimalApi.TodoList.AddingItems;

namespace MinimalApi.TodoList.NotifyWhenItemAdded;

public static class Configuration
{
    public static IBeckettBuilder UseNotifyWhenItemAdded(this IBeckettBuilder builder)
    {
        builder.Services.AddScoped<TodoListItemAddedHandler>();

        builder.AddSubscription<TodoListItemAddedHandler, TodoListItemAdded>(
            nameof(TodoListItemAddedHandler),
            (handler, message, token) => handler.Handle(message, token),
            configuration => configuration.StartingPosition = StartingPosition.Earliest
        );

        return builder;
    }
}
