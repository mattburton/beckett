namespace Taskmaster.TaskLists.GetList;

public record GetListQuery(Guid Id)
{
    public async Task<TaskListReadModel?> Execute(
        IMessageStore messageStore,
        CancellationToken cancellationToken
    )
    {
        var stream = await messageStore.ReadStream(TaskList.StreamName(Id), cancellationToken);

        return stream.IsEmpty ? null : stream.ProjectTo<TaskListReadModel>();
    }
}
