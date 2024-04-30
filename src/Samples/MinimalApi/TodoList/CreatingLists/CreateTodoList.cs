namespace MinimalApi.TodoList.CreatingLists;

public record CreateTodoList(Guid Id, string Name)
{
    public Task<IAppendResult> Execute(IEventStore eventStore, CancellationToken cancellationToken)
    {
        return eventStore.AppendToStream(
            StreamName.For<TodoList>(Id),
            ExpectedVersion.StreamDoesNotExist,
            new TodoListCreated(Id, Name),
            cancellationToken
        );
    }
}
