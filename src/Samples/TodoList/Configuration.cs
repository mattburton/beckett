using TodoList.AddItem;
using TodoList.CompleteItem;
using TodoList.CreateList;
using TodoList.Notifications.Activity;

namespace TodoList;

public static class Configuration
{
    public static IBeckettBuilder UseTodoListModule(this IBeckettBuilder builder)
    {
        builder.MapMessage<TodoListCreated>("TodoListCreated");
        builder.MapMessage<TodoListItemAdded>("TodoListItemAdded");
        builder.MapMessage<TodoListItemCompleted>("TodoListItemCompleted");

        builder.AddSubscription(
            "Notifications:ActivityNotificationHandler",
            ActivityNotificationHandler.Handle,
            configuration =>
            {
                configuration.StartingPosition = StartingPosition.Earliest;

                configuration.SubscribeTo<TodoListCreated>();
                configuration.SubscribeTo<TodoListItemAdded>();
            });

        return builder;
    }
}
