using Core.Queries;
using TaskHub.TaskLists.Slices.TaskLists;

namespace API.V1.TaskLists;

public static class GetTaskListsEndpoint
{
    public static async Task<IResult> Handle(IQueryBus queryBus, CancellationToken cancellationToken)
    {
        var results = await queryBus.Send(new TaskListsQuery(), cancellationToken);

        return Results.Ok(results);
    }
}
