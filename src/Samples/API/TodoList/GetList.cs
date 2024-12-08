using Beckett;
using TodoList.GetList;

namespace API.TodoList;

public static class GetList
{
    public static RouteGroupBuilder GetListRoute(this RouteGroupBuilder builder)
    {
        builder.MapGet("/{id:guid}", Handler);

        return builder;
    }

    private static async Task<IResult> Handler(
        Guid id,
        IMessageStore messageStore,
        CancellationToken cancellationToken
    )
    {
        var model = await new GetTodoList(id).Execute(messageStore, cancellationToken);

        return model == null ? Results.NotFound() : Results.Ok(model);
    }
}
