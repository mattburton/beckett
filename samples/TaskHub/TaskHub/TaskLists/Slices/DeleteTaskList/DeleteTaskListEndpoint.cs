namespace TaskHub.TaskLists.Slices.DeleteTaskList;

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
