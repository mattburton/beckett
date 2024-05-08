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
            (handler, @event, token) => handler.Handle(@event, token),
            configuration => configuration.StartingPosition = StartingPosition.Earliest
        );

        return builder;
    }
}
