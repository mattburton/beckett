namespace TaskHub.TaskLists.Slices.DeleteTaskList;

public static class DeleteTaskListEndpoint
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
                new DeleteTaskListCommand(id),
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
