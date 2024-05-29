using TodoList.AddItem;
using TodoList.CompleteItem;
using TodoList.CreateList;

namespace TodoList.Notifications;

public class NotificationHandler
{
    public Task Handle(IMessageContext context, CancellationToken _)
    {
        switch (context.Message)
        {
            case TodoListCreated e:
                Console.WriteLine($"List created [Id: {e.TodoListId}]");
                break;
            case TodoListItemAdded e:
                if (e.Item.Contains("Error"))
                {
                    throw new Exception(e.Item);
                }

                Console.WriteLine($"Item added: {e.Item} [List: {e.TodoListId}]");
                break;
            case TodoListItemCompleted e:
                Console.WriteLine($"Item completed: {e.Item} [List: {e.TodoListId}]");
                break;
        }

        return Task.CompletedTask;
    }
}
