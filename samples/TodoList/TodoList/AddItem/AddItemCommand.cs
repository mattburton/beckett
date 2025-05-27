using TodoList.Events;

namespace TodoList.AddItem;

public record AddItemCommand(Guid Id, string Item)
{
    public async Task<IAppendResult> Execute(IMessageStore messageStore, CancellationToken cancellationToken)
    {
        var stream = await messageStore.ReadStream(TodoList.StreamName(Id), cancellationToken);

        var state = stream.ProjectTo<DecisionState>();

        if (state.Items.Contains(Item))
        {
            throw new ItemAlreadyAddedException();
        }

        return await stream.Append(
            new Message(new TodoListItemAdded(Id, Item)).WithCorrelationId(Guid.NewGuid().ToString()),
            cancellationToken
        );
    }

    private class DecisionState : IApply
    {
        public HashSet<string> Items { get; } = [];

        public void Apply(IMessageContext context)
        {
            switch (context.Message)
            {
                case TodoListItemAdded e:
                    Apply(e);
                    break;
            }
        }

        private void Apply(TodoListItemAdded e) => Items.Add(e.Item);
    }
}
