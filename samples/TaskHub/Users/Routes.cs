using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Users.DeleteUser;
using Users.GetUser;
using Users.GetUsers;
using Users.RegisterUser;

namespace Users;

public static class Routes
{
    public static IEndpointRouteBuilder MapUserRoutes(this IEndpointRouteBuilder builder)
    {
        var routes = builder.MapGroup("users");

        routes.MapDelete("/{username}", DeleteUserEndpoint.Handle);
        routes.MapGet("/{username}", GetUserEndpoint.Handle);
        routes.MapGet("/", GetUsersEndpoint.Handle);
        routes.MapPost("/", RegisterUserEndpoint.Handle);

        return builder;
    }
}
