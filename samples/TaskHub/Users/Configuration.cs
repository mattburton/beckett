using Users.GetUsers;
using Users.PublishEvent;

namespace Users;

public class Configuration : IModuleConfiguration
{
    public string ModuleName => "User";

    public void Configure(IModuleBuilder builder)
    {
        builder.AddBatchProcessor<PublishEventProcessor>()
            .Category(UserStream.Category)
            .StreamScope(StreamScope.GlobalStream);

        builder.AddProjection<GetUsersProjection, GetUsersReadModel>();
    }
}
