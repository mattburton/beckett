using TaskHub.Infrastructure.Modules;
using TaskHub.Infrastructure.Routing;
using TaskHub.Users.Contracts.Notifications;
using TaskHub.Users.Events;
using TaskHub.Users.Slices.DeleteUser;
using TaskHub.Users.Slices.NotifyUserAdded;
using TaskHub.Users.Slices.RegisterUser;
using TaskHub.Users.Slices.User;
using TaskHub.Users.Slices.Users;

namespace TaskHub.Users;

public class UserModule : IModule, IConfigureRoutes
{
    private const string Category = "user";

    public static string StreamName(string username) => $"{Category}-{username}";

    public void MessageTypes(IMessageTypeBuilder builder)
    {
        // events
        builder.Map<UserRegistered>("user_registered");
        builder.Map<UserDeleted>("user_deleted");

        // notifications
        builder.Map<UserAddedNotification>("user_added_notification");
    }

    public void Subscriptions(ISubscriptionBuilder builder)
    {
        builder.AddSubscription("users:users_projection")
            .Projection<UsersProjection, UsersReadModel, string>();

        builder.AddSubscription("users:notifications:user_added:user_registered")
            .Message<UserRegistered>()
            .Handler(UserAddedNotificationPublisher.UserRegistered);

        builder.AddSubscription("users:wire_tap")
            .Category(Category)
            .Handler(
                (IMessageContext context) =>
                {
                    Console.WriteLine($"[MESSAGE] category: {Category}, stream: {context.StreamName}, type: {context.Type}, id: {context.Id} [lag: {DateTimeOffset.UtcNow.Subtract(context.Timestamp).TotalMilliseconds} ms]");

                    return Task.CompletedTask;
                }
            );
    }

    public void Routes(IEndpointRouteBuilder builder)
    {
        var routes = builder.MapGroup("users");

        routes.MapGet("/", GetUsersEndpoint.Handle);
        routes.MapPost("/", RegisterUserEndpoint.Handle);
        routes.MapGet("/{username}", GetUserEndpoint.Handle);
        routes.MapDelete("/{username}", DeleteUserEndpoint.Handle);
    }
}
