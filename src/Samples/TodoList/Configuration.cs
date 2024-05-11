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

        builder.MapMessage<TodoListCreated>("todo_list_created");
        builder.MapMessage<TodoListItemAdded>("todo_list_item_added");
        builder.MapMessage<TodoListItemCompleted>("todo_list_item_completed");

        builder.AddSubscription<MentionsHandler, TodoListItemAdded>(
            "mentions",
            (handler, message, token) => handler.Handle(message, token)
        );

        builder.AddSubscription(
            "notifications",
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
