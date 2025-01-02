using TaskHub.Infrastructure.Routing;
using TaskHub.TaskLists.AddList;
using TaskHub.TaskLists.AddTask;
using TaskHub.TaskLists.ChangeListName;
using TaskHub.TaskLists.CompleteTask;
using TaskHub.TaskLists.DeleteList;
using TaskHub.TaskLists.GetList;
using TaskHub.TaskLists.GetLists;

namespace TaskHub.TaskLists;

public class Routes : IConfigureRoutes
{
    public void Configure(IEndpointRouteBuilder builder)
    {
        builder.MapPost("/", AddListHandler.Post);
        builder.MapGet("/", GetListsHandler.Get);
        builder.MapPost("/{taskListId:guid}", AddTaskHandler.Post);
        builder.MapPut("/{id:guid}/name", ChangeListNameHandler.Post);
        builder.MapPost("/{taskListId:guid}/complete/{task}", CompleteTaskHandler.Post);
        builder.MapDelete("/{id:guid}", DeleteListHandler.Delete);
        builder.MapGet("/{id:guid}", GetListHandler.Get);

    }
}
