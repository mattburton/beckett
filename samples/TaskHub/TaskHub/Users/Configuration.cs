using TaskHub.Users.Processors.PublishNotification;
using TaskHub.Users.Queries.GetUsers;
using TaskHub.Users.Streams;

namespace TaskHub.Users;

public class Configuration : IModuleConfiguration
{
    public void Configure(IModuleBuilder builder)
    {
        builder.AddProcessor<PublishNotificationProcessor>(UserStream.Category);
        builder.AddProjection<UsersProjection, UsersReadModel, string>();
    }
}
