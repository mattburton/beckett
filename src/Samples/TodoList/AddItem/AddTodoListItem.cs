namespace TodoList.AddItem;

public record AddTodoListItem(Guid Id, string Item)
{
    public async Task<AppendResult> Execute(IMessageStore messageStore, CancellationToken cancellationToken)
    {
        var stream = await messageStore.ReadStream(TodoList.StreamName(Id), cancellationToken);

        var state = stream.ProjectTo<DecisionState>();

        if (state.Items.Contains(Item))
        {
            throw new ItemAlreadyAddedException();
        }

        return await stream.Append(
            new TodoListItemAdded(Id, Item),
            cancellationToken
        );
    }

    private class DecisionState : IApply
    {
        public HashSet<string> Items { get; } = [];

        public void Apply(object message)
        {
            switch (message)
            {
                case TodoListItemAdded e:
                    Apply(e);
                    break;
            }
        }

        private void Apply(TodoListItemAdded e) => Items.Add(e.Item);
    }
}
