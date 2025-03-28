using Contracts.TaskLists.Commands;
using Contracts.TaskLists.Exceptions;

namespace API.V1.TaskLists;

public static class CompleteTaskEndpoint
{
    public static async Task<IResult> Handle(
        Guid taskListId,
        string task,
        ITaskListModule module,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await module.Execute(new CompleteTaskCommand(taskListId, task), cancellationToken);

            return Results.Ok(new Response(taskListId, task));
        }
        catch (TaskAlreadyCompletedException)
        {
            return Results.Conflict();
        }
    }

    public record Response(Guid TaskListId, string Task);
}
