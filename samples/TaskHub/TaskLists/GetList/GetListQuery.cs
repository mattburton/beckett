namespace TaskLists.GetList;

public record GetListQuery(Guid Id) : IQuery<GetListReadModel>
{
    public class Handler(IStreamReader reader) : StreamStateQueryHandler<GetListQuery, GetListReadModel>(reader)
    {
        protected override IStreamName StreamName(GetListQuery query) => new TaskListStream(query.Id);
    }
}
