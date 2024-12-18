namespace Taskmaster.TaskLists.CreateList;

public record CreateListCommand(Guid Id, string Name)
{
    public Task<AppendResult> Execute(IMessageStore messageStore, CancellationToken cancellationToken) =>
        messageStore.AppendToStream(
            TaskList.StreamName(Id),
            ExpectedVersion.StreamDoesNotExist,
            new TaskListCreated(Id, Name),
            cancellationToken
        );
}
