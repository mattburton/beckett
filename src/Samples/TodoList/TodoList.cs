using TodoList.AddItem;
using TodoList.Mentions;
using TodoList.Notifications;

namespace TodoList;

public static class TodoList
{
    private const string Category = nameof(TodoList);

    public static string StreamName(Guid id) => $"{Category}-{id}";

    public static IBeckettBuilder TodoListModule(this IBeckettBuilder builder)
    {
        builder.Services.AddTransient<MentionsHandler>();

        builder.AddSubscription("mentions")
            .Category(Category)
            .Message<TodoListItemAdded>()
            .Handler<MentionsHandler>((handler, message, token) => handler.Handle(message, token));

        builder.AddSubscription("notifications")
            .Category(Category)
            .Handler(NotificationHandler.Handle);

        return builder;
    }
}
