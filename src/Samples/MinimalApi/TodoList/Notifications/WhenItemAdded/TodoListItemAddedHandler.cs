using MinimalApi.TodoList.AddingItems;

namespace MinimalApi.TodoList.Notifications.WhenItemAdded;

public class TodoListItemAddedHandler
{
    public Task Handle(TodoListItemAdded e, CancellationToken _)
    {
        Console.WriteLine($"Item was added to list: {e.Item} [List: {e.TodoListId}]");

        return Task.CompletedTask;
    }
}