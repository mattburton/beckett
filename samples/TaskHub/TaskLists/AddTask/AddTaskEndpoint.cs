namespace TaskLists.AddTask;

public static class AddTaskEndpoint
{
    public static async Task<IResult> Handle(
        Guid id,
        Request request,
        ITaskListModule module,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await module.Execute(new AddTaskCommand(id, request.Task), cancellationToken);

            return Results.Ok(new Response(id, request.Task));
        }
        catch (TaskAlreadyAddedException)
        {
            return Results.Conflict();
        }
    }

    public record Request(string Task);

    public record Response(Guid TaskListId, string Task);
}
