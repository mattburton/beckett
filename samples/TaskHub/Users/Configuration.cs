using Users.GetUsers;
using Users.PublishEvent;

namespace Users;

public class Configuration : IModuleConfiguration
{
    public string ModuleName => "User";

    public void Configure(IModuleBuilder builder)
    {
        builder.AddProcessor<PublishEventProcessor>(UserStream.Category, nameof(PublishEventProcessor));

        builder.AddProjection<GetUsersProjection, GetUsersReadModel, string>("GetUsersQueryProjection");
    }
}
