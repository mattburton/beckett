using Contracts.TaskLists.Commands;

namespace API.V1.TaskLists;

public static class ChangeTaskListNameEndpoint
{
    public static async Task<IResult> Handle(
        Guid id,
        Request request,
        ITaskListModule module,
        CancellationToken cancellationToken
    )
    {
        await module.Execute(new ChangeTaskListNameCommand(id, request.Name), cancellationToken);

        return Results.Ok(new Response(id, request.Name));
    }

    public record Request(string Name);

    public record Response(Guid Id, string Name);
}
