namespace TodoList.CreateList;

public record CreateTodoList(Guid Id, string Name)
{
    public Task<AppendResult> Execute(IMessageStore messageStore, CancellationToken cancellationToken) =>
        messageStore.AppendToStream(
            TodoList.StreamName(Id),
            ExpectedVersion.StreamDoesNotExist,
            new TodoListCreated(Id, Name),
            cancellationToken
        );
}
