namespace TaskLists.AddList;

public static class AddListEndpoint
{
    public static async Task<IResult> Handle(
        Request request,
        ITaskListModule module,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await module.Execute(new AddListCommand(request.Id, request.Name), cancellationToken);

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
