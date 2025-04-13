using Users.GetUsers;
using Users.PublishEvent;

namespace Users;

public class Configuration : IModuleConfiguration
{
    public string ModuleName => "User";

    public void Configure(IModuleBuilder builder)
    {
        builder.AddProcessor<PublishEventProcessor>(UserStream.Category);

        builder.AddProjection<GetUsersProjection, GetUsersReadModel>();
    }
}
