namespace TaskHub.TaskLists.Slices.TaskList;

public class TaskListQueryHandler(
    IMessageStore messageStore
) : ProjectedStreamQueryHandler<TaskListQuery, TaskListReadModel>(messageStore)
{
    protected override string StreamName(TaskListQuery query) => TaskListModule.StreamName(query.Id);
}
