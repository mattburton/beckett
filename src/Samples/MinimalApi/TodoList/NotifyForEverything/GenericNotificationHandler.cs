using MinimalApi.TodoList.AddingItems;
using MinimalApi.TodoList.CreatingLists;

namespace MinimalApi.TodoList.NotifyForEverything;

public class GenericNotificationHandler
{
    public Task Handle(IMessageContext context, CancellationToken _)
    {
        switch (context.Message)
        {
            case TodoListCreated e:
                Console.WriteLine($"GENERIC: Todo list was created [List: {e.TodoListId}]");
                break;
            case TodoListItemAdded e:
                Console.WriteLine($"GENERIC: Item was added to list: {e.Item} [List: {e.TodoListId}]");
                break;
        }

        return Task.CompletedTask;
    }
}
