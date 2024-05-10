namespace TodoList.AddItem;

public record AddTodoListItem(string Item)
{
    public async Task<AppendResult> Execute(Guid id, IMessageStore messageStore, CancellationToken cancellationToken)
    {
        var streamName = StreamName.For<TodoList>(id);

        var stream = await messageStore.ReadStream(streamName, cancellationToken);

        var state = stream.ProjectTo<DecisionState>();

        if (state.Items.Contains(Item))
        {
            throw new ItemAlreadyAddedException();
        }

        return await messageStore.AppendToStream(
            streamName,
            ExpectedVersion.For(stream.StreamVersion),
            new TodoListItemAdded(id, Item),
            cancellationToken
        );
    }

    private class DecisionState : IState
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

        private void Apply(TodoListItemAdded e)
        {
            Items.Add(e.Item);
        }
    }
}
