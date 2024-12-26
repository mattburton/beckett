namespace TaskHub.TaskList.CompleteTask;

public class CompleteTaskHandler
{
    public static async Task<IResult> Post(
        Guid taskListId,
        string task,
        ICommandExecutor commandExecutor,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await commandExecutor.Execute(
                TaskList.StreamName(taskListId),
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


