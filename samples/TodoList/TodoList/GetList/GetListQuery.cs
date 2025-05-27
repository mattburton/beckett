namespace TodoList.GetList;

public record GetListQuery(Guid Id)
{
    public async Task<GetListReadModel?> Execute(IMessageStore messageStore, CancellationToken cancellationToken)
    {
        var stream = await messageStore.ReadStream(TodoList.StreamName(Id), cancellationToken);

        return stream.IsEmpty ? null : stream.ProjectTo<GetListReadModel>();
    }
}
