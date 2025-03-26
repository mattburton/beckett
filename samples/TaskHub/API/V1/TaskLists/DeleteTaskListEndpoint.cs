using Beckett;
using Core.Commands;
using TaskHub.TaskLists.Slices.DeleteTaskList;

namespace API.V1.TaskLists;

public static class DeleteTaskListEndpoint
{
    public static async Task<IResult> Handle(
        Guid id,
        ICommandBus commandBus,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await commandBus.Send(new DeleteTaskListCommand(id), cancellationToken);

            return Results.Ok();
        }
        catch (StreamDoesNotExistException)
        {
            return Results.Conflict();
        }
    }
}
