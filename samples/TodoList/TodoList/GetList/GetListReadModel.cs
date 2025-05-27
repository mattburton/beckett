using TodoList.Events;

namespace TodoList.GetList;

public class GetListReadModel : IApply
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public Dictionary<string, bool> Items { get; } = [];

    public void Apply(IMessageContext context)
    {
        switch (context.Message)
        {
            case TodoListCreated e:
                Apply(e);
                break;
            case TodoListItemAdded e:
                Apply(e);
                break;
            case TodoListItemCompleted e:
                Apply(e);
                break;
        }
    }

    private void Apply(TodoListCreated e)
    {
        Id = e.TodoListId;
        Name = e.Name;
    }

    private void Apply(TodoListItemAdded e) => Items.Add(e.Item, false);

    private void Apply(TodoListItemCompleted e) => Items[e.Item] = true;
}
