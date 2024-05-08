using MinimalApi.TodoList.AddingItems;

namespace MinimalApi.TodoList.NotifyWhenItemAdded;

public class TodoListItemAddedHandler
{
    public Task Handle(TodoListItemAdded message, CancellationToken _)
    {
        var sampleErrorNotThrown = string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SAMPLE_ERROR_THROWN"));

        if (message.Item == "Error" && sampleErrorNotThrown)
        {
            Environment.SetEnvironmentVariable("SAMPLE_ERROR_THROWN", "true");

            throw new Exception("Error");
        }

        Console.WriteLine($"Item was added to list: {message.Item} [List: {message.TodoListId}]");

        return Task.CompletedTask;
    }
}
