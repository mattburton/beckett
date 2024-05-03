using MinimalApi.TodoList.AddingItems;

namespace MinimalApi.TodoList.Notifications.WhenItemAdded;

public class Configuration : IConfigureBeckett
{
    public void Configure(IServiceCollection services, BeckettOptions beckett)
    {
        services.AddScoped<TodoListItemAddedHandler>();

        beckett.Subscriptions.AddSubscription<TodoListItemAddedHandler, TodoListItemAdded>(
            nameof(TodoListItemAddedHandler),
            (handler, @event, token) => handler.Handle(@event, token)
        );
    }
}
