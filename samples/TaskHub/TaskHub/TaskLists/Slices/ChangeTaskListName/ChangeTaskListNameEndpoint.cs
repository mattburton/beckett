namespace TaskHub.TaskLists.Slices.ChangeTaskListName;

public static class ChangeTaskListNameEndpoint
{
    public static async Task<IResult> Handle(
        Guid id,
        Request request,
        ICommandBus commandBus,
        CancellationToken cancellationToken
    )
    {
        await commandBus.Send(new ChangeTaskListNameCommand(id, request.Name), cancellationToken);

        return Results.Ok(new Response(id, request.Name));
    }

    public record Request(string Name);

    public record Response(Guid Id, string Name);
}
