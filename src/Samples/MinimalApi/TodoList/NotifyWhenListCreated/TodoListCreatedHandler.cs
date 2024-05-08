using MinimalApi.TodoList.CreatingLists;

namespace MinimalApi.TodoList.NotifyWhenListCreated;

public class TodoListCreatedHandler
{
    public Task Handle(TodoListCreated message, CancellationToken _)
    {
        Console.WriteLine($"Todo list was created: {message.Name} [ID: {message.TodoListId}]");

        return Task.CompletedTask;
    }
}
