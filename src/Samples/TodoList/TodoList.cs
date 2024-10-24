using Microsoft.Extensions.DependencyInjection;
using TodoList.AddItem;
using TodoList.CompleteItem;
using TodoList.CreateList;
using TodoList.Mentions;
using TodoList.Notifications;
using TodoList.ScheduledTasks;

namespace TodoList;

public static class TodoList
{
    private const string Category = nameof(TodoList);

    public static string StreamName(Guid id) => $"{Category}-{id}";

    public static IBeckettBuilder TodoListMessages(this IBeckettBuilder builder)
    {
        builder.Map<TodoListItemAdded>("todo_list_item_added");
        builder.Map<TodoListItemCompleted>("todo_list_item_completed");
        builder.Map<TodoListCreated>("todo_list_created");
        builder.Map<TodoListCreated>("todo_list_created");
        builder.Map<HelloWorld>("hello_world");

        return builder;
    }

    public static IBeckettBuilder TodoListSubscriptions(this IBeckettBuilder builder)
    {
        builder.Services.AddSingleton<MentionsHandler>();
        builder.Services.AddSingleton<NotificationHandler>();
        builder.Services.AddSingleton<HelloWorld.Handler>();

        builder.TodoListMessages();

        builder.AddSubscription("Mentions")
            .Message<TodoListItemAdded>()
            .Handler<MentionsHandler>((handler, message, token) => handler.Handle(message, token));

        builder.AddSubscription("Notifications")
            .Category(Category)
            .Handler<NotificationHandler>((handler, context, token) => handler.Handle(context, token));

        builder.AddRecurringMessage(nameof(HelloWorld), "* * * * *", nameof(HelloWorld), new HelloWorld());

        builder.AddSubscription("HelloWorld")
            .Message<HelloWorld>()
            .Handler<HelloWorld.Handler>((handler, message, token) => handler.Handle(message, token));

        return builder;
    }
}
