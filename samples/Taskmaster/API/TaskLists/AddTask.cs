using Beckett.Commands;
using Taskmaster.TaskLists;
using Taskmaster.TaskLists.AddTask;

namespace API.TaskLists;

public static class AddTask
{
    public static async Task<IResult> Handler(
        Guid id,
        Request request,
        ICommandInvoker commandInvoker,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var result = await commandInvoker.Execute(
                TaskList.StreamName(id),
                new AddTaskCommand(id, request.Item),
                cancellationToken
            );

            return Results.Ok(new Response(id, request.Item, result.StreamVersion));
        }
        catch (TaskAlreadyAddedException)
        {
            return Results.Conflict();
        }
    }

    public record Request(string Item);

    private record Response(Guid Id, string Item, long StreamVersion);
}
