namespace TodoList.CompleteItem;

public record CompleteTodoListItem(Guid Id, string Item)
{
    public async Task<AppendResult> Execute(IMessageStore messageStore, CancellationToken cancellationToken)
    {
        var stream = await messageStore.ReadStream(Topics.TodoList, Id, cancellationToken);

        var state = stream.ProjectTo<DecisionState>();

        if (state.CompletedItems.Contains(Item))
        {
            throw new ItemAlreadyCompletedException();
        }

        return await messageStore.AppendToStream(
            Topics.TodoList,
            Id,
            ExpectedVersion.For(stream.StreamVersion),
            new TodoListItemCompleted(Id, Item),
            cancellationToken
        );
    }

    private class DecisionState : IState
    {
        public HashSet<string> CompletedItems { get; } = new();

        public void Apply(object message)
        {
            if (message is TodoListItemCompleted e)
            {
                CompletedItems.Add(e.Item);
            }
        }
    }
}
