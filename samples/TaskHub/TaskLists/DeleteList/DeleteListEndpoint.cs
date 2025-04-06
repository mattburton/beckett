namespace TaskLists.DeleteList;

public static class DeleteListEndpoint
{
    public static async Task<IResult> Handle(Guid id, ITaskListModule module, CancellationToken cancellationToken)
    {
        try
        {
            await module.Execute(new DeleteListCommand(id), cancellationToken);

            return Results.Ok();
        }
        catch (StreamDoesNotExistException)
        {
            return Results.Conflict();
        }
    }
}
