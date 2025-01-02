namespace TaskHub.TaskLists.DeleteList;

public class DeleteListHandler
{
    public static async Task<IResult> Delete(
        Guid id,
        ICommandExecutor commandExecutor,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await commandExecutor.Execute(
                TaskList.StreamName(id),
                new DeleteListCommand(id),
                new CommandOptions
                {
                    ExpectedVersion = ExpectedVersion.StreamExists
                },
                cancellationToken
            );

            return Results.Ok();
        }
        catch (StreamDoesNotExistException)
        {
            return Results.Conflict();
        }
    }
}
