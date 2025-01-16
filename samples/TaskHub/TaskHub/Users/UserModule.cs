using TaskHub.Infrastructure.Routing;
using TaskHub.Users.Contracts.Notifications;
using TaskHub.Users.Events;
using TaskHub.Users.Slices.DeleteUser;
using TaskHub.Users.Slices.GetUser;
using TaskHub.Users.Slices.GetUsers;
using TaskHub.Users.Slices.RegisterUser;
using TaskHub.Users.Slices.UserAddedNotification;

namespace TaskHub.Users;

public class UserModule : IBeckettModule, IConfigureRoutes
{
    public static string StreamName(string username) => $"user-{username}";

    public static string NotificationStreamName(string username) => $"user_notifications-{username}";

    public void MessageTypes(IMessageTypeBuilder builder)
    {
        builder.Map<UserRegistered>("user_registered");
        builder.Map<UserDeleted>("user_deleted");

        builder.Map<UserAddedNotification>("user_added_notification");
    }

    public void Subscriptions(ISubscriptionBuilder builder)
    {
        builder.AddSubscription("users:get_users_projection")
            .Projection<GetUsersProjection, GetUsersReadModel, string>();

        builder.AddSubscription("users:user_added_notification:user_registered")
            .Message<UserRegistered>()
            .Handler(UserAddedNotificationPublisher.UserRegistered);
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
