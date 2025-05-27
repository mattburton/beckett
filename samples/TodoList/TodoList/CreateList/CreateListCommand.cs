using TodoList.Events;

namespace TodoList.CreateList;

public record CreateListCommand(Guid Id, string Name)
{
    public Task<IAppendResult> Execute(IMessageStore messageStore, CancellationToken cancellationToken) =>
        messageStore.AppendToStream(
            TodoList.StreamName(Id),
            ExpectedVersion.StreamDoesNotExist,
            new TodoListCreated(Id, Name),
            cancellationToken
        );
}
