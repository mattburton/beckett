namespace TaskHub.TaskLists.Slices.AddTask;

public static class AddTaskEndpoint
{
    public static async Task<IResult> Handle(
        Guid taskListId,
        Request request,
        ICommandExecutor commandExecutor,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await commandExecutor.Execute(
                TaskListModule.StreamName(taskListId),
                new AddTaskCommand(taskListId, request.Task),
                cancellationToken
            );

            return Results.Ok(new Response(taskListId, request.Task));
        }
        catch (TaskAlreadyAddedException)
        {
            return Results.Conflict();
        }
    }

    public record Request(string Task);

    public record Response(Guid TaskListId, string Task);
}
