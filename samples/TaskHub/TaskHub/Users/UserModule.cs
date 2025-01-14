using TaskHub.Infrastructure.Routing;
using TaskHub.Users.Events;
using TaskHub.Users.Slices.DeleteUser;
using TaskHub.Users.Slices.GetUser;
using TaskHub.Users.Slices.GetUsers;
using TaskHub.Users.Slices.RegisterUser;

namespace TaskHub.Users;

public class UserModule : IBeckettModule, IConfigureRoutes
{
    public static string StreamName(string username) => $"user-{username}";

    public void MessageTypes(IMessageTypeBuilder builder)
    {
        builder.Map<UserRegistered>("user_registered");
        builder.Map<UserDeleted>("user_deleted");
    }

    public void Subscriptions(ISubscriptionBuilder builder)
    {
        builder.AddSubscription("users:get_users_read_model_projection")
            .Projection<GetUsersReadModelProjection, GetUsersReadModel, string>();
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
