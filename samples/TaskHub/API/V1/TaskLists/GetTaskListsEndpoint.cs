using Contracts.TaskLists.Queries;

namespace API.V1.TaskLists;

public static class GetTaskListsEndpoint
{
    public static async Task<IResult> Handle(ITaskListModule module, CancellationToken cancellationToken)
    {
        var results = await module.Execute(new GetTaskLists(), cancellationToken);

        return Results.Ok(results);
    }
}
