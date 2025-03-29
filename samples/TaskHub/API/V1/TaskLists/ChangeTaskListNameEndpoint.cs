using Contracts.TaskLists.Commands;

namespace API.V1.TaskLists;

public static class ChangeTaskListNameEndpoint
{
    public static async Task<IResult> Handle(
        Guid taskListId,
        Request request,
        ITaskListModule module,
        CancellationToken cancellationToken
    )
    {
        await module.Execute(new ChangeTaskListName(taskListId, request.Name), cancellationToken);

        return Results.Ok(new Response(taskListId, request.Name));
    }

    public record Request(string Name);

    public record Response(Guid Id, string Name);
}
