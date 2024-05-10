using TodoList.AddItem;
using TodoList.CompleteItem;
using TodoList.CreateList;

namespace TodoList.Notifications.Activity;

public static class ActivityNotificationHandler
{
    public static Task Handle(IMessageContext context, CancellationToken _)
    {
        switch (context.Message)
        {
            case TodoListCreated e:
                Console.WriteLine($"List created [Id: {e.TodoListId}]");
                break;
            case TodoListItemAdded e:
                Console.WriteLine($"Item was added: {e.Item} [List: {e.TodoListId}]");
                break;
            case TodoListItemCompleted e:
                Console.WriteLine($"Item was completed: {e.Item} [List: {e.TodoListId}]");
                break;
        }

        return Task.CompletedTask;
    }
}
