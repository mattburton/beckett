using Beckett;
using Beckett.Commands;
using Taskmaster.TaskLists;
using Taskmaster.TaskLists.CreateList;

namespace API.TaskLists;

public static class CreateList
{
    public static async Task<IResult> Handler(
        Request request,
        ICommandInvoker commandInvoker,
        CancellationToken cancellationToken
    )
    {
        var result = await commandInvoker.Execute(
            TaskList.StreamName(request.Id),
            new CreateListCommand(request.Id, request.Name),
            new CommandOptions
            {
                ExpectedVersion = ExpectedVersion.StreamDoesNotExist
            },
            cancellationToken
        );

        return Results.Ok(new Response(request.Id, request.Name, result.StreamVersion));
    }

    public record Request(Guid Id, string Name);

    private record Response(Guid Id, string Name, long StreamVersion);
}
