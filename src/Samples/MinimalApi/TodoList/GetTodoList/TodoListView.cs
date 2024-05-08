using MinimalApi.TodoList.AddingItems;
using MinimalApi.TodoList.CreatingLists;

namespace MinimalApi.TodoList.GetTodoList;

public class TodoListView : IState
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public List<string> Items { get; set; } = [];

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
        }
    }

    private void Apply(TodoListCreated e)
    {
        Id = e.TodoListId;
        Name = e.Name;
    }

    private void Apply(TodoListItemAdded e)
    {
        Items.Add(e.Item);
    }
}
