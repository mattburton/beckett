using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.GetLists;

namespace TaskHub.TaskLists;

public class TaskList : IBeckettModule
{
    private const string Category = "TaskList";

    public static string StreamName(Guid id) => $"{Category}-{id}";

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

        builder.AddSubscription("task_list:wire_tap")
            .Category(Category)
            .Handler(
                (IMessageContext context) =>
                {
                    Console.WriteLine($"[MESSAGE] type: {context.Type}, id: {context.Id}");

                    return Task.CompletedTask;
                }
            );
    }
}
