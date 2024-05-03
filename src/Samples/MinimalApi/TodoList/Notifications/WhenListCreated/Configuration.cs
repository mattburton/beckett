using MinimalApi.TodoList.CreatingLists;

namespace MinimalApi.TodoList.Notifications.WhenListCreated;

public class Configuration : IConfigureBeckett
{
    public void Configure(IServiceCollection services, BeckettOptions beckett)
    {
        services.AddScoped<TodoListCreatedHandler>();

        beckett.Subscriptions.AddSubscription<TodoListCreatedHandler, TodoListCreated>(
            nameof(TodoListCreatedHandler),
            (handler, @event, token) => handler.Handle(@event, token)
        );
    }
}
