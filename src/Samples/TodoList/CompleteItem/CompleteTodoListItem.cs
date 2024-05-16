namespace TodoList.CompleteItem;

public record CompleteTodoListItem(Guid Id, string Item)
{
    public async Task<AppendResult> Execute(IMessageStore messageStore, CancellationToken cancellationToken)
    {
        var stream = await messageStore.ReadStream(TodoList.StreamName(Id), cancellationToken);

        var state = stream.ProjectTo<DecisionState>();

        if (state.CompletedItems.Contains(Item))
        {
            throw new ItemAlreadyCompletedException();
        }

        return await messageStore.AppendToStream(
            TodoList.StreamName(Id),
            ExpectedVersion.For(stream.StreamVersion),
            new TodoListItemCompleted(Id, Item),
            cancellationToken
        );
    }

    private class DecisionState : IApply
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
