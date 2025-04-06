namespace TaskLists.CompleteTask;

public static class CompleteTaskEndpoint
{
    public static async Task<IResult> Handle(
        Guid id,
        string task,
        ITaskListModule module,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await module.Execute(new CompleteTaskCommand(id, task), cancellationToken);

            return Results.Ok(new Response(id, task));
        }
        catch (TaskAlreadyCompletedException)
        {
            return Results.Conflict();
        }
    }

    public record Response(Guid TaskListId, string Task);
}
