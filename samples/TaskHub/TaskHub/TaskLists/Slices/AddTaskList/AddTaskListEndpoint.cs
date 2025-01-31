namespace TaskHub.TaskLists.Slices.AddTaskList;

public static class AddTaskListEndpoint
{
    public static async Task<IResult> Handle(
        Request request,
        ICommandBus commandBus,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await commandBus.Send(new AddTaskListCommand(request.Id, request.Name), cancellationToken);

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
