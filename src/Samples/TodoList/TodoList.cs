using Microsoft.Extensions.DependencyInjection;
using TodoList.AddItem;
using TodoList.CompleteItem;
using TodoList.CreateList;
using TodoList.Mentions;
using TodoList.Notifications;

namespace TodoList;

public class TodoList : IBeckettModule
{
    private const string Category = nameof(TodoList);

    public static string StreamName(Guid id) => $"{Category}-{id}";

    public void MessageTypes(IBeckettBuilder builder)
    {
        builder.Map<TodoListItemAdded>("todo_list_item_added");
        builder.Map<TodoListItemCompleted>("todo_list_item_completed");
        builder.Map<TodoListCreated>("todo_list_created");
    }

    public void ClientConfiguration(IBeckettBuilder builder)
    {
    }

    public void ServerConfiguration(IBeckettBuilder builder)
    {
        builder.Services.AddSingleton<MentionsHandler>();
        builder.Services.AddSingleton<NotificationHandler>();

        builder.AddSubscription("Mentions")
            .Message<TodoListItemAdded>()
            .Handler<MentionsHandler>(static (handler, message, token) => handler.Handle(message, token));

        builder.AddSubscription("Notifications")
            .Category(Category)
            .Handler<NotificationHandler>(static (handler, context, token) => handler.Handle(context, token));
    }
}
