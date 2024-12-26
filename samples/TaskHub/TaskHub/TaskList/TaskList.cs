using TaskHub.TaskList.Events;
using TaskHub.TaskList.GetLists;

namespace TaskHub.TaskList;

public class TaskList : IBeckettModule
{
    public static string StreamName(Guid id) => $"TaskList-{id}";

    public void MessageTypes(IMessageTypeBuilder builder)
    {
        builder.Map<TaskAdded>("task_added");
        builder.Map<TaskCompleted>("task_completed");
        builder.Map<TaskListAdded>("task_list_added");
        builder.Map<TaskListNameChanged>("task_list_name_changed");
        builder.Map<TaskListDeleted>("task_list_deleted");
    }

    public void Subscriptions(ISubscriptionBuilder builder)
    {
        builder.AddSubscription("task_list:get_lists_projection")
            .Projection<GetListsProjection, GetListsReadModel, Guid>();
    }
}
