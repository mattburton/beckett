using TodoList.Events;

namespace TodoList.CompleteItem;

public record CompleteItemCommand(Guid Id, string Item)
{
    public async Task<IAppendResult> Execute(IMessageStore messageStore, CancellationToken cancellationToken)
    {
        var stream = await messageStore.ReadStream(TodoList.StreamName(Id), cancellationToken);

        var state = stream.ProjectTo<DecisionState>();

        if (state.CompletedItems.Contains(Item))
        {
            throw new ItemAlreadyCompletedException();
        }

        return await stream.Append(
            new TodoListItemCompleted(Id, Item),
            cancellationToken
        );
    }

    private class DecisionState : IApply
    {
        public HashSet<string> CompletedItems { get; } = new();

        public void Apply(IMessageContext context)
        {
            if (context.Message is TodoListItemCompleted e)
            {
                CompletedItems.Add(e.Item);
            }
        }
    }
}
