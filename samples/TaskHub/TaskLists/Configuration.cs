using TaskLists.Events;
using TaskLists.GetLists;
using TaskLists.NotifyUser;

namespace TaskLists;

public class Configuration : IModuleConfiguration
{
    public string ModuleName => "TaskList";

    public void Configure(IModuleBuilder builder)
    {
        builder.AddProcessor<NotifyUserProcessor, UserMentionedInTask>();

        builder.AddProjection<UserLookupProjection, UserLookupReadModel>();

        builder.AddProjection<GetListsProjection, GetListsReadModel>();
    }
}
