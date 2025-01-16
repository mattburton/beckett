namespace TaskHub.TaskLists.Slices.GetList;

public record GetListQuery(Guid Id) : IQuery<GetListReadModel>;
