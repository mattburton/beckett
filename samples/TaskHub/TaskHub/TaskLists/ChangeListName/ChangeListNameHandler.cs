namespace TaskHub.TaskLists.ChangeListName;

public class ChangeListNameHandler
{
    public static async Task<IResult> Post(
        Guid id,
        Request request,
        ICommandExecutor commandExecutor,
        CancellationToken cancellationToken
    )
    {
        await commandExecutor.Execute(
            TaskList.StreamName(id),
            new ChangeListNameCommand(id, request.Name),
            cancellationToken
        );

        return Results.Ok(new Response(id, request.Name));
    }

    public record Request(string Name);

    public record Response(Guid Id, string Name);
}
