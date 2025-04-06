using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using TaskLists.AddList;
using TaskLists.AddTask;
using TaskLists.ChangeListName;
using TaskLists.CompleteTask;
using TaskLists.DeleteList;
using TaskLists.GetList;
using TaskLists.GetLists;

namespace TaskLists;

public static class Routes
{
    public static IEndpointRouteBuilder MapTaskListRoutes(this IEndpointRouteBuilder builder)
    {
        var routes = builder.MapGroup("task-lists");

        routes.MapPost("/", AddListEndpoint.Handle);
        routes.MapPost("/{id:guid}/tasks", AddTaskEndpoint.Handle);
        routes.MapPut("/{id:guid}/name", ChangeListNameEndpoint.Handle);
        routes.MapPost("/{id:guid}/tasks/{task}/complete", CompleteTaskEndpoint.Handle);
        routes.MapDelete("/{id:guid}", DeleteListEndpoint.Handle);
        routes.MapGet("/{id:guid}", GetListEndpoint.Handle);
        routes.MapGet("/", GetListsEndpoint.Handle);

        return builder;
    }
}
