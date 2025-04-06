namespace TaskLists.GetLists;

public static class GetListsEndpoint
{
    public static async Task<IResult> Handle(ITaskListModule module, CancellationToken cancellationToken)
    {
        var results = await module.Execute(new GetListsQuery(), cancellationToken);

        return Results.Ok(Response.From(results));
    }

    public record Response(List<Response.TaskList> TaskLists)
    {
        public static Response From(IReadOnlyList<GetListsReadModel>? results)
        {
            return new Response(results?.Select(x => new TaskList(x.Id, x.Name)).ToList() ?? []);
        }

        public record TaskList(Guid Id, string Name);
    }
}
