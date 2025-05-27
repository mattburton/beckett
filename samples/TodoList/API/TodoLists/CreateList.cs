using TodoList.CreateList;

namespace API.TodoLists;

public static class CreateList
{
    public static void Route(RouteGroupBuilder builder)
    {
        builder.MapPost("/", Handler);
    }

    private static async Task<Results<Ok<CreateListResponse>, Conflict>> Handler(
        CreateListRequest request,
        IMessageStore messageStore,
        CancellationToken cancellationToken
    )
    {
        var command = new CreateListCommand(request.Id, request.Name);

        var result = await command.Execute(messageStore, cancellationToken);

        try
        {
            return TypedResults.Ok(new CreateListResponse(request.Id, request.Name, result.StreamVersion));
        }
        catch (StreamAlreadyExistsException)
        {
            return TypedResults.Conflict();
        }
    }
}

[UsedImplicitly]
public record CreateListRequest(Guid Id, string Name);

[UsedImplicitly]
public record CreateListResponse(Guid Id, string Name, long StreamVersion);
