using Microsoft.Extensions.DependencyInjection;
using TodoList.AddItem;
using TodoList.Mentions;
using TodoList.Notifications;
using TodoList.ScheduledTasks;

namespace TodoList;

public static class TodoList
{
    private const string Category = nameof(TodoList);

    public static string StreamName(Guid id) => $"{Category}-{id}";

    public static IBeckettBuilder TodoListSubscriptions(this IBeckettBuilder builder)
    {
        builder.Services.AddTransient<MentionsHandler>();
        builder.Services.AddTransient<NotificationHandler>();
        builder.Services.AddTransient<HelloWorld>();

        builder.AddSubscription("Mentions")
            .Category(Category)
            .Message<TodoListItemAdded>()
            .Handler<MentionsHandler>((handler, message, token) => handler.Handle(message, token));

        builder.AddSubscription("Notifications")
            .Category(Category)
            .Handler<NotificationHandler>((handler, context, token) => handler.Handle(context, token))
            .MaxRetryCount<NotificationHandler.TerminalException>(0);

        builder.ScheduleRecurringMessage(nameof(HelloWorld), "* * * * *", nameof(HelloWorld), new HelloWorld());

        builder.AddSubscription("HelloWorld")
            .Category(nameof(HelloWorld))
            .Message<HelloWorld>()
            .Handler<HelloWorld>((handler, message, token) => handler.Handle(message, token));

        return builder;
    }
}
