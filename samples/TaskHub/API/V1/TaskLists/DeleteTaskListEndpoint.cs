using Contracts.TaskLists.Commands;

namespace API.V1.TaskLists;

public static class DeleteTaskListEndpoint
{
    public static async Task<IResult> Handle(
        Guid taskListId,
        ITaskListModule module,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await module.Execute(new DeleteTaskListCommand(taskListId), cancellationToken);

            return Results.Ok();
        }
        catch (ResourceNotFoundException)
        {
            return Results.Conflict();
        }
    }
}
