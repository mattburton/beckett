using Contracts.TaskLists.Queries;
using Core.Streams;
using TaskHub.TaskLists.Streams;

namespace TaskHub.TaskLists.Queries.GetTaskList;

public class GetTaskListQueryHandler(
    IStreamReader reader
) : IQueryHandler<GetTaskListQuery, GetTaskListQuery.Result?>
{
    public async Task<GetTaskListQuery.Result?> Handle(GetTaskListQuery query, CancellationToken cancellationToken)
    {
        var stream = await reader.ReadStream(new TaskListStream(query.Id), cancellationToken);

        if (stream.IsEmpty)
        {
            return null;
        }

        var model = stream.ProjectTo<TaskListReadModel>();

        return Map(model);
    }

    private static GetTaskListQuery.Result Map(TaskListReadModel model) =>
        new(
            model.Id,
            model.Name,
            model.Tasks.Select(x => new GetTaskListQuery.TaskItem(x.Task, x.Completed)).ToList()
        );
}
