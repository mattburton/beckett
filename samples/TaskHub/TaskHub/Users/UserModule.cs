using TaskHub.Infrastructure.Modules;
using TaskHub.Infrastructure.Routing;
using TaskHub.Users.Events;
using TaskHub.Users.Notifications;
using TaskHub.Users.Slices.DeleteUser;
using TaskHub.Users.Slices.PublishNotification;
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
        builder.Map<UserRegistered>("user_registered");
        builder.Map<UserDeleted>("user_deleted");

        builder.Map<UserNotification>("user_notification");
    }

    public void Subscriptions(ISubscriptionBuilder builder)
    {
        builder.AddSubscription("users:users_projection")
            .Projection<UsersProjection, UsersReadModel, string>();

        builder.AddSubscription("users:user_notification_publisher")
            .Category(Category)
            .Handler(UserNotificationPublisher.Handle);

        builder.AddSubscription("users:wire_tap")
            .Category(Category)
            .Handler(
                (IMessageContext context) =>
                {
                    Console.WriteLine(
                        $"[MESSAGE] category: {Category}, stream: {context.StreamName}, type: {context.Type}, id: {context.Id} [lag: {DateTimeOffset.UtcNow.Subtract(context.Timestamp).TotalMilliseconds} ms]"
                    );
                }
            );
    }

    public void Routes(IEndpointRouteBuilder builder)
    {
        var routes = builder.MapGroup("users");

        routes.MapGet("/", UsersEndpoint.Handle);
        routes.MapPost("/", RegisterUserEndpoint.Handle);
        routes.MapGet("/{username}", UserEndpoint.Handle);
        routes.MapDelete("/{username}", DeleteUserEndpoint.Handle);
    }
}
