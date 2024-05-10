using TodoList.AddItem;
using TodoList.CompleteItem;
using TodoList.CreateList;
using TodoList.Mentions;
using TodoList.Notifications;

namespace TodoList;

public static class Configuration
{
    public static IBeckettBuilder UseTodoListModule(this IBeckettBuilder builder)
    {
        builder.Services.AddTransient<MentionsHandler>();

        builder.MapMessage<TodoListCreated>("TodoListCreated");
        builder.MapMessage<TodoListItemAdded>("TodoListItemAdded");
        builder.MapMessage<TodoListItemCompleted>("TodoListItemCompleted");

        builder.AddSubscription<MentionsHandler, TodoListItemAdded>(
            "Mentions",
            (handler, message, token) => handler.Handle(message, token)
        );

        builder.AddSubscription(
            "Notifications",
            NotificationHandler.Handle,
            configuration =>
            {
                configuration.SubscribeTo<TodoListCreated>();
                configuration.SubscribeTo<TodoListItemAdded>();
                configuration.SubscribeTo<TodoListItemCompleted>();
            });

        return builder;
    }
}
