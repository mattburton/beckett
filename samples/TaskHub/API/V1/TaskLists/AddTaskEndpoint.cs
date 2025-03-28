using Contracts.TaskLists.Commands;
using Contracts.TaskLists.Exceptions;

namespace API.V1.TaskLists;

public static class AddTaskEndpoint
{
    public static async Task<IResult> Handle(
        Guid taskListId,
        Request request,
        ITaskListModule module,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await module.Execute(new AddTaskCommand(taskListId, request.Task), cancellationToken);

            return Results.Ok(new Response(taskListId, request.Task));
        }
        catch (TaskAlreadyAddedException)
        {
            return Results.Conflict();
        }
    }

    public record Request(string Task);

    public record Response(Guid TaskListId, string Task);
}
