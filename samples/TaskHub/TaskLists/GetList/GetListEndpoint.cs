namespace TaskLists.GetList;

public static class GetListEndpoint
{
    public static async Task<IResult> Handle(Guid id, ITaskListModule module, CancellationToken cancellationToken)
    {
        var result = await module.Execute(new GetListQuery(id), cancellationToken);

        return result == null ? Results.NotFound() : Results.Ok(Response.From(result));
    }

    public record Response(Guid Id, string Name, List<Response.TaskItem> Tasks)
    {
        public static Response From(GetListReadModel result)
        {
            return new Response(
                result.Id,
                result.Name,
                result.Tasks.Select(x => new TaskItem(x.Task, x.Completed)).ToList()
            );
        }

        public record TaskItem(string Task, bool Completed);
    }
}
