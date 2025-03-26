using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Slices.NotifyUser;
using TaskHub.TaskLists.Slices.TaskLists;
using TaskHub.TaskLists.Slices.UserLookup;

namespace TaskHub.TaskLists;

public class TaskListModule : IModule
{
    private const string Category = "task_list";

    public static string StreamName(Guid id) => $"{Category}-{id}";

    public void MessageTypes(IMessageTypeBuilder builder)
    {
        builder.Map<TaskAdded>("task_added");
        builder.Map<TaskCompleted>("task_completed");
        builder.Map<TaskListAdded>("task_list_added");
        builder.Map<TaskListNameChanged>("task_list_name_changed");
        builder.Map<TaskListDeleted>("task_list_deleted");
        builder.Map<UserMentionedInTask>("user_mentioned_in_task");
        builder.Map<UserNotificationSent>("user_notification_sent");
    }

    public void Subscriptions(ISubscriptionBuilder builder)
    {
        builder.AddSubscription("task_lists:task_lists_projection")
            .Projection<TaskListsProjection, TaskListsReadModel, Guid>();

        builder.AddSubscription("task_lists:user_lookup_projection")
            .Projection<UserLookupProjection, UserLookupReadModel, string>();

        builder.AddSubscription("task_lists:notify_user")
            .Message<UserMentionedInTask>()
            .Handler(UserMentionedInTaskHandler.Handle);

        builder.AddSubscription("task_lists:wire_tap")
            .Category(Category)
            .Handler(
                (IMessageContext context) =>
                {
                    Console.WriteLine(
                        $"[MESSAGE] category: {Category}, stream: {context.StreamName}, type: {context.Type}, id: {context.Id} [lag: {DateTimeOffset.UtcNow.Subtract(context.Timestamp).TotalMilliseconds} ms]"
                    );
                }
            );
    }
}
