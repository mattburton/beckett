using MinimalApi.TodoList.AddingItems;

namespace MinimalApi.TodoList.Notifications.WhenItemAdded;

public class Configuration : IConfigureBeckett
{
    public void Configure(IServiceCollection services, BeckettOptions options)
    {
        services.AddScoped<TodoListItemAddedHandler>();

        options.Subscriptions.AddSubscription<TodoListItemAddedHandler, TodoListItemAdded>(
            nameof(TodoListItemAddedHandler),
            (handler, @event, token) => handler.Handle(@event, token)
        );
    }
}
