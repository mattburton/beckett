namespace TaskHub.TaskLists.Slices.DeleteList;

public static class DeleteListEndpoint
{
    public static async Task<IResult> Handle(
        Guid id,
        ICommandExecutor commandExecutor,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await commandExecutor.Execute(
                TaskListModule.StreamName(id),
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
