using Contracts.TaskLists.Commands;

namespace API.V1.TaskLists;

public static class AddTaskListEndpoint
{
    public static async Task<IResult> Handle(
        Request request,
        ITaskListModule module,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await module.Execute(new AddTaskListCommand(request.Id, request.Name), cancellationToken);

            return Results.Ok(new Response(request.Id, request.Name));
        }
        catch (ResourceAlreadyExistsException)
        {
            return Results.Conflict();
        }
    }

    public record Request(Guid Id, string Name);

    public record Response(Guid Id, string Name);
}
