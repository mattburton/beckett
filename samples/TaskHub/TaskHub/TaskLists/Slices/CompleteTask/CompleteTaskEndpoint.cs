namespace TaskHub.TaskLists.Slices.CompleteTask;

public static class CompleteTaskEndpoint
{
    public static async Task<IResult> Handle(
        Guid taskListId,
        string task,
        ICommandExecutor commandExecutor,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await commandExecutor.Execute(
                TaskListModule.StreamName(taskListId),
                new CompleteTaskCommand(taskListId, task),
                cancellationToken
            );

            return Results.Ok(new Response(taskListId, task));
        }
        catch (TaskAlreadyCompletedException)
        {
            return Results.Conflict();
        }
    }

    public record Response(Guid TaskListId, string Task);
}
