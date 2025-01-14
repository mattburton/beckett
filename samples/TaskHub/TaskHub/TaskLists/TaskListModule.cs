using TaskHub.Infrastructure.Routing;
using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Slices.AddList;
using TaskHub.TaskLists.Slices.AddTask;
using TaskHub.TaskLists.Slices.ChangeListName;
using TaskHub.TaskLists.Slices.CompleteTask;
using TaskHub.TaskLists.Slices.DeleteList;
using TaskHub.TaskLists.Slices.GetList;
using TaskHub.TaskLists.Slices.GetLists;
using TaskHub.TaskLists.Slices.UserMentionNotification;

namespace TaskHub.TaskLists;

public class TaskListModule : IBeckettModule, IConfigureRoutes
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
        builder.Map<UserMentionNotificationSent>("user_mention_notification_sent");
    }

    public void Subscriptions(ISubscriptionBuilder builder)
    {
        builder.AddSubscription("task_lists:get_lists_read_model_projection")
            .Projection<GetListsReadModelProjection, GetListsReadModel, Guid>();

        builder.AddSubscription("task_lists:user_mention_notification")
            .Message<UserMentionedInTask>()
            .Handler(UserMentionedInTaskHandler.Handle);

        builder.AddSubscription("task_lists:wire_tap")
            .Category(Category)
            .Handler(
                (IMessageContext context) =>
                {
                    Console.WriteLine($"[MESSAGE] type: {context.Type}, id: {context.Id}");

                    return Task.CompletedTask;
                }
            );
    }

    public void Routes(IEndpointRouteBuilder builder)
    {
        var routes = builder.MapGroup("task-lists");

        routes.MapPost("/", AddListEndpoint.Handle);
        routes.MapGet("/", GetListsEndpoint.Handle);
        routes.MapPost("/{taskListId:guid}", AddTaskEndpoint.Handle);
        routes.MapPut("/{id:guid}/name", ChangeListNameEndpoint.Handle);
        routes.MapPost("/{taskListId:guid}/complete/{task}", CompleteTaskEndpoint.Handle);
        routes.MapDelete("/{id:guid}", DeleteListEndpoint.Handle);
        routes.MapGet("/{id:guid}", GetListEndpoint.Handle);
    }
}
