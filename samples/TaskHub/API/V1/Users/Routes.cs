namespace API.V1.Users;

public static class Routes
{
    public static RouteGroupBuilder MapUserRoutes(this RouteGroupBuilder builder)
    {
        var routes = builder.MapGroup("users");

        routes.MapPost("/", RegisterUserEndpoint.Handle);
        routes.MapGet("/", GetUsersEndpoint.Handle);
        routes.MapGet("/{username}", GetUserEndpoint.Handle);
        routes.MapDelete("/{username}", DeleteUserEndpoint.Handle);

        return builder;
    }
}
