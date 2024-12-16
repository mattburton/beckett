using Beckett;
using TodoList.CreateList;

namespace API.TodoList;

public static class CreateList
{
    public static RouteGroupBuilder CreateListRoute(this RouteGroupBuilder builder)
    {
        builder.MapPost("/", Handler);

        return builder;
    }

    private static async Task<IResult> Handler(
        CreateListRequest request,
        IMessageStore messageStore,
        CancellationToken cancellationToken
    )
    {
        var result = await new CreateTodoList(request.Id, request.Name).Execute(messageStore, cancellationToken);

        return Results.Ok(new CreateListResponse(request.Id, request.Name, result.StreamVersion));
    }
}

public record CreateListRequest(Guid Id, string Name);

public record CreateListResponse(Guid Id, string Name, long StreamVersion);
