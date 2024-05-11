namespace TodoList.AddItem;

public record AddTodoListItem(string Item)
{
    public async Task<AppendResult> Execute(Guid id, IMessageStore messageStore, CancellationToken cancellationToken)
    {
       var stream = await messageStore.ReadStream(Topics.TodoList, id, cancellationToken);

        var state = stream.ProjectTo<DecisionState>();

        if (state.Items.Contains(Item))
        {
            throw new ItemAlreadyAddedException();
        }

        return await messageStore.AppendToStream(
            Topics.TodoList,
            id,
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
