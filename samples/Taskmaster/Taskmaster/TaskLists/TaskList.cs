using Taskmaster.TaskLists.AddTask;
using Taskmaster.TaskLists.CompleteTask;
using Taskmaster.TaskLists.CreateList;
using Taskmaster.TaskLists.Mentions;
using Taskmaster.TaskLists.Notifications;

namespace Taskmaster.TaskLists;

public class TaskList : IBeckettModule
{
    private const string Category = nameof(TaskList);

    public static string StreamName(Guid id) => $"{Category}-{id}";

    public void MessageTypes(IMessageTypeBuilder builder)
    {
        builder.Map<TaskAdded>("task_added");
        builder.Map<TaskCompleted>("task_completed");
        builder.Map<TaskListCreated>("todo_list_created");
    }

    public void Subscriptions(ISubscriptionBuilder builder)
    {
        builder.AddSubscription("Mentions")
            .Message<TaskAdded>()
            .Handler<MentionsHandler>();

        builder.AddSubscription("Notifications")
            .Category(Category)
            .Handler<NotificationHandler>();
    }
}
