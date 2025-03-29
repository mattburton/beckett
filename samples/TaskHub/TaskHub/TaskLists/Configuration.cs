using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Processors.NotifyUser;
using TaskHub.TaskLists.Queries;

namespace TaskHub.TaskLists;

public class Configuration : IModuleConfiguration
{
    public string ModuleName => "TaskList";

    public void Configure(IModuleBuilder builder)
    {
        builder.AddProcessor<NotifyUserProcessor, UserMentionedInTask>(nameof(NotifyUserProcessor));
        builder.AddProjection<UserLookup.Projection, UserLookup.State, string>("UserLookupQueryProjection");
        builder.AddProjection<GetTaskListsHandler.Projection, GetTaskListsHandler.State, Guid>(
            "GetTaskListsQueryProjection"
        );
    }
}
