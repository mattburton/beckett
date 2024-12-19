using Taskmaster.TaskLists.AddTask;
using Taskmaster.TaskLists.CompleteTask;
using Taskmaster.TaskLists.CreateList;
using TaskAddedHandler = Taskmaster.TaskLists.AssignReviewTask.TaskAddedHandler;

namespace Taskmaster.TaskLists;

public class TaskList : IBeckettModule
{
    private const string Category = nameof(TaskList);

    public static string StreamName(Guid id) => $"{Category}-{id}";

    public void MessageTypes(IMessageTypeBuilder builder)
    {
        builder.Map<TaskAdded>("task_added");
        builder.Map<TaskCompleted>("task_completed");
        builder.Map<TaskListCreated>("task_list_created");
    }

    public void Subscriptions(ISubscriptionBuilder builder)
    {
        builder.AddSubscription("task_lists:assign_review_task")
            .Message<TaskAdded>()
            .Handler<TaskAddedHandler>();
    }
}
