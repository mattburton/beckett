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
        Request request,
        IMessageStore messageStore,
        CancellationToken cancellationToken
    )
    {
        var result = await new CreateTodoList(request.Id, request.Name).Execute(messageStore, cancellationToken);

        return Results.Ok(
            new
            {
                request.Id,
                request.Name,
                result.StreamVersion
            }
        );
    }

    public record Request(Guid Id, string Name);
}
