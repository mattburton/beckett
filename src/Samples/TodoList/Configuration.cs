using TodoList.AddItem;
using TodoList.CompleteItem;
using TodoList.CreateList;
using TodoList.Mentions;
using TodoList.Notifications;

namespace TodoList;

public static class Configuration
{
    public static IBeckettBuilder TodoListMessageMap(this IBeckettBuilder builder)
    {
        builder.Map<TodoListCreated>("todo_list_created");
        builder.Map<TodoListItemAdded>("todo_list_item_added");
        builder.Map<TodoListItemCompleted>("todo_list_item_completed");

        return builder;
    }

    public static IBeckettBuilder TodoListComponent(this IBeckettBuilder builder)
    {
        builder.TodoListMessageMap();

        builder.Services.AddTransient<MentionsHandler>();

        builder.AddSubscription("mentions")
            .Topic(Topics.TodoList)
            .Message<TodoListItemAdded>()
            .Handler<MentionsHandler>((handler, message, token) => handler.Handle(message, token));

        builder.AddSubscription("notifications")
            .Topic(Topics.TodoList)
            .Message<TodoListItemAdded>()
            .Handler(NotificationHandler.Handle);

        return builder;
    }
}
