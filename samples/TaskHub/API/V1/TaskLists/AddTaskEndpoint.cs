using Core.Commands;
using TaskHub.TaskLists.Slices.AddTask;

namespace API.V1.TaskLists;

public static class AddTaskEndpoint
{
    public static async Task<IResult> Handle(
        Guid taskListId,
        Request request,
        ICommandBus commandBus,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await commandBus.Send(new AddTaskCommand(taskListId, request.Task), cancellationToken);

            return Results.Ok(new Response(taskListId, request.Task));
        }
        catch (TaskAlreadyAddedException)
        {
            return Results.Conflict();
        }
    }

    public record Request(string Task);

    public record Response(Guid TaskListId, string Task);
}
