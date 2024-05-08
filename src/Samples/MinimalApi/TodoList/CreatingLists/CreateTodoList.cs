namespace MinimalApi.TodoList.CreatingLists;

public record CreateTodoList(Guid Id, string Name)
{
    public Task<AppendResult> Execute(IMessageStore messageStore, CancellationToken cancellationToken)
    {
        return messageStore.AppendToStream(
            StreamName.For<TodoList>(Id),
            ExpectedVersion.StreamDoesNotExist,
            new TodoListCreated(Id, Name),
            cancellationToken
        );
    }
}
