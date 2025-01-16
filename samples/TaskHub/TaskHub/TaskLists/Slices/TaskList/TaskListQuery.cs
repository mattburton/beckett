namespace TaskHub.TaskLists.Slices.TaskList;

public record TaskListQuery(Guid Id) : IQuery<TaskListReadModel>;
