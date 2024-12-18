using Beckett.Commands;
using Taskmaster.TaskLists;
using Taskmaster.TaskLists.CompleteTask;

namespace API.TaskLists;

public static class CompleteTask
{
    public static async Task<IResult> Handler(
        Guid id,
        string item,
        ICommandInvoker commandInvoker,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var result = await commandInvoker.Execute(
                TaskList.StreamName(id),
                new CompleteTaskCommand(id, item),
                cancellationToken
            );

            return Results.Ok(new Response(id, item, result.StreamVersion));
        }
        catch (TaskAlreadyCompletedException)
        {
            return Results.Conflict();
        }
    }

    private record Response(Guid Id, string Item, long StreamVersion);
}


