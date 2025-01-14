namespace TaskHub.TaskLists.Slices.GetList;

public record GetListQuery(Guid Id) : IQuery<GetListReadModel>
{
    public class Handler(
        IMessageStore messageStore
    ) : ProjectedStreamQueryHandler<GetListQuery, GetListReadModel>(messageStore)
    {
        protected override string StreamName(GetListQuery query) => TaskListModule.StreamName(query.Id);
    }
}
