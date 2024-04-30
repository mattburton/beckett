namespace MinimalApi.TodoList.AddingItems;

public record AddTodoListItem(Guid Id, string Item)
{
    public async Task<IAppendResult> Execute(IEventStore eventStore, CancellationToken cancellationToken)
    {
        var stream = await eventStore.ReadStream(StreamName.For<TodoList>(Id), cancellationToken);

        var state = stream.ProjectTo<DecisionState>();

        if (state.Items.Contains(Item))
        {
            throw new ItemAlreadyAddedException();
        }

        return await eventStore.AppendToStream(
            StreamName.For<TodoList>(Id),
            new ExpectedVersion(stream.StreamVersion),
            new TodoListItemAdded(Id, Item),
            cancellationToken
        );
    }
}
