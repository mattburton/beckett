using API.V1.TaskLists;
using API.V1.Users;

namespace API.V1;

public static class Routes
{
    public static RouteGroupBuilder MapV1Routes(this RouteGroupBuilder builder)
    {
        return builder
            .MapTaskListRoutes()
            .MapUserRoutes();
    }
}
