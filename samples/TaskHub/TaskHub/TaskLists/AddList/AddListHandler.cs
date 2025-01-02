namespace TaskHub.TaskLists.AddList;

public static class AddListHandler
{
    public static async Task<IResult> Post(
        Request request,
        ICommandExecutor commandExecutor,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await commandExecutor.Execute(
                TaskList.StreamName(request.Id),
                new AddListCommand(request.Id, request.Name),
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
