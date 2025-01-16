namespace TaskHub.TaskLists.Slices.AddTaskList;

public static class AddTaskListEndpoint
{
    public static async Task<IResult> Handle(
        Request request,
        ICommandExecutor commandExecutor,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await commandExecutor.Execute(
                TaskListModule.StreamName(request.Id),
                new AddTaskListCommand(request.Id, request.Name),
                new CommandOptions
                {
                    ExpectedVersion = ExpectedVersion.StreamDoesNotExist
                },
                cancellationToken
            );

            return Results.Ok(new Response(request.Id, request.Name));
        }
        catch (StreamAlreadyExistsException)
        {
            return Results.Conflict();
        }
    }

    public record Request(Guid Id, string Name);

    public record Response(Guid Id, string Name);
}
