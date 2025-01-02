namespace TaskHub.TaskLists.GetList;

public record GetListQuery(Guid Id)
{
    public async Task<GetListReadModel?> Execute(
        IMessageStore messageStore,
        CancellationToken cancellationToken
    )
    {
        var stream = await messageStore.ReadStream(TaskList.StreamName(Id), cancellationToken);

        return stream.IsEmpty ? null : stream.ProjectTo<GetListReadModel>();
    }
}
