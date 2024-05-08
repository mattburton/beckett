namespace MinimalApi.TodoList.AddingItems;

public class DecisionState : IState
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
