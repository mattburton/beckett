using TaskLists.Events;
using TaskLists.GetLists;
using TaskLists.NotifyUser;

namespace TaskLists;

public class Configuration : IModuleConfiguration
{
    public string ModuleName => "TaskList";

    public void Configure(IModuleBuilder builder)
    {
        builder.AddProcessor<NotifyUserProcessor, UserMentionedInTask>(nameof(NotifyUserProcessor));

        builder.AddProjection<UserLookupProjection, UserLookupReadModel, string>("UserLookupQueryProjection");

        builder.AddProjection<GetListsProjection, GetListsReadModel, Guid>(
            "GetTaskListsQueryProjection"
        );
    }
}
