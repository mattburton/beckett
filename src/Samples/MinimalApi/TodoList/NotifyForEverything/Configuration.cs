using Beckett.Subscriptions;
using MinimalApi.TodoList.AddingItems;
using MinimalApi.TodoList.CreatingLists;

namespace MinimalApi.TodoList.NotifyForEverything;

public static class Configuration
{
    public static IBeckettBuilder UseGenericNotify(this IBeckettBuilder builder)
    {
        builder.AddSubscription(
            nameof(GenericNotificationHandler),
            GenericNotificationHandler.Handle,
            configuration =>
            {
                configuration.StartingPosition = StartingPosition.Earliest;

                configuration.SubscribeTo<TodoListCreated>();
                configuration.SubscribeTo<TodoListItemAdded>();
            });

        return builder;
    }
}
