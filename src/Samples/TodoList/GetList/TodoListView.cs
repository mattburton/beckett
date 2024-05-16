using TodoList.AddItem;
using TodoList.CompleteItem;
using TodoList.CreateList;

namespace TodoList.GetList;

public class TodoListView : IApply
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public Dictionary<string, bool> Items { get; set; } = [];

    public void Apply(object message)
    {
        switch (message)
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

    private void Apply(TodoListItemAdded e)
    {
        Items.Add(e.Item, false);
    }

    private void Apply(TodoListItemCompleted e)
    {
        Items[e.Item] = true;
    }
}
