using TaskHub.Infrastructure.Routing;
using TaskHub.TaskList.AddList;
using TaskHub.TaskList.AddTask;
using TaskHub.TaskList.ChangeListName;
using TaskHub.TaskList.CompleteTask;
using TaskHub.TaskList.DeleteList;
using TaskHub.TaskList.GetList;
using TaskHub.TaskList.GetLists;

namespace TaskHub.TaskList;

public class Routes : IConfigureRoutes
{
    public void Configure(IEndpointRouteBuilder builder)
    {
        builder.MapPost("/", AddListHandler.Post);
        builder.MapPost("/{taskListId:guid}", AddTaskHandler.Post);
        builder.MapPut("/{id:guid}/name", ChangeListNameHandler.Post);
        builder.MapPost("/{taskListId:guid}/complete/{task}", CompleteTaskHandler.Post);
        builder.MapDelete("/{id:guid}", DeleteListHandler.Delete);
        builder.MapGet("/{id:guid}", GetListHandler.Get);
        builder.MapGet("/", GetListsHandler.Get);
    }
}
