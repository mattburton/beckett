namespace MinimalApi.TodoList.AddingItems;

public record AddTodoListItem(Guid Id, string Item)
{
    public Task<IAppendResult> Execute(IEventStore eventStore, CancellationToken cancellationToken)
    {
        return eventStore.AppendToStream(
            StreamName.For<TodoList>(Id),
            ExpectedVersion.StreamExists,
            new TodoListItemAdded(Id, Item),
            cancellationToken
        );
    }
}
