namespace MinimalApi.TodoList.AddingItems;

public record AddTodoListItem(Guid Id, string Item)
{
    public async Task<AppendResult> Execute(IMessageStore messageStore, CancellationToken cancellationToken)
    {
        var streamName = StreamName.For<TodoList>(Id);

        var stream = await messageStore.ReadStream(streamName, cancellationToken);

        var state = stream.AggregateTo<DecisionState>();

        if (state.Items.Contains(Item))
        {
            throw new ItemAlreadyAddedException();
        }

        return await messageStore.AppendToStream(
            streamName,
            ExpectedVersion.For(stream.StreamVersion),
            new TodoListItemAdded(Id, Item),
            cancellationToken
        );
    }
}
