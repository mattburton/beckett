using Beckett;
using Taskmaster.TaskLists.CreateList;

namespace API.TaskLists;

public static class CreateList
{
    public static async Task<IResult> Handler(
        Request request,
        IMessageStore messageStore,
        CancellationToken cancellationToken
    )
    {
        var result = await new CreateListCommand(request.Id, request.Name).Execute(messageStore, cancellationToken);

        return Results.Ok(new Response(request.Id, request.Name, result.StreamVersion));
    }

    public record Request(Guid Id, string Name);

    private record Response(Guid Id, string Name, long StreamVersion);
}
