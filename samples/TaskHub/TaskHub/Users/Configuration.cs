using TaskHub.Users.Processors;
using TaskHub.Users.Queries;
using TaskHub.Users.Streams;

namespace TaskHub.Users;

public class Configuration : IModuleConfiguration
{
    public string ModuleName => "User";

    public void Configure(IModuleBuilder builder)
    {
        builder.AddProcessor<PublishNotificationProcessor>(UserStream.Category, nameof(PublishNotificationProcessor));
        builder.AddProjection<GetUsersHandler.Projection, GetUsersHandler.State, string>("GetUsersQueryProjection");
    }
}
