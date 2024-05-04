namespace MinimalApi.TodoList.AddingItems;

public record AddTodoListItem(Guid Id, string Item)
{
    public async Task<AppendResult> Execute(IEventStore eventStore, CancellationToken cancellationToken)
    {
        var streamName = StreamName.For<TodoList>(Id);

        var stream = await eventStore.ReadStream(streamName, cancellationToken);

        var state = stream.ProjectTo<DecisionState>();

        if (state.Items.Contains(Item))
        {
            throw new ItemAlreadyAddedException();
        }

        return await eventStore.AppendToStream(
            streamName,
            ExpectedVersion.For(stream.StreamVersion),
            new TodoListItemAdded(Id, Item),
            cancellationToken
        );
    }
}
