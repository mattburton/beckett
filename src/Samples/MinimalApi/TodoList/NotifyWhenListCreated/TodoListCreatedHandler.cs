using MinimalApi.TodoList.CreatingLists;

namespace MinimalApi.TodoList.NotifyWhenListCreated;

public class TodoListCreatedHandler
{
    public Task Handle(TodoListCreated e, CancellationToken _)
    {
        Console.WriteLine($"Todo list was created: {e.Name} [ID: {e.TodoListId}]");

        return Task.CompletedTask;
    }
}
