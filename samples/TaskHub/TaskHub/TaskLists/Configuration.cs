using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Processors.NotifyUser;
using TaskHub.TaskLists.Queries.GetTaskLists;

namespace TaskHub.TaskLists;

public class Configuration : IModuleConfiguration
{
    public void Configure(IModuleBuilder builder)
    {
        builder.AddProcessor<NotifyUserProcessor, UserMentionedInTask>();
        builder.AddProjection<UserLookupProjection, UserLookupReadModel, string>();
        builder.AddProjection<TaskListsProjection, TaskListsReadModel, Guid>();
    }
}
