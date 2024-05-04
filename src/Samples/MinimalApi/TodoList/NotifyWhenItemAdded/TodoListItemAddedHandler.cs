using MinimalApi.TodoList.AddingItems;

namespace MinimalApi.TodoList.NotifyWhenItemAdded;

public class TodoListItemAddedHandler
{
    public Task Handle(TodoListItemAdded e, CancellationToken _)
    {
        var sampleErrorNotThrown = string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SAMPLE_ERROR_THROWN"));

        if (e.Item == "Error" && sampleErrorNotThrown)
        {
            Environment.SetEnvironmentVariable("SAMPLE_ERROR_THROWN", "true");

            throw new Exception("Error");
        }

        Console.WriteLine($"Item was added to list: {e.Item} [List: {e.TodoListId}]");

        return Task.CompletedTask;
    }
}
