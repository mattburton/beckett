namespace TaskHub.TaskLists.Slices.GetList;

public class GetListQueryHandler(
    IMessageStore messageStore
) : ProjectedStreamQueryHandler<GetListQuery, GetListReadModel>(messageStore)
{
    protected override string StreamName(GetListQuery query) => TaskListModule.StreamName(query.Id);
}
