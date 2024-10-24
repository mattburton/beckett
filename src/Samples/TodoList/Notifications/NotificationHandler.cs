using Microsoft.Extensions.Logging;
using TodoList.AddItem;
using TodoList.CompleteItem;
using TodoList.CreateList;

namespace TodoList.Notifications;

public class NotificationHandler(ILogger<NotificationHandler> logger)
{
    public Task Handle(IMessageContext context, CancellationToken _)
    {
        switch (context.Message)
        {
            case TodoListCreated e:
                logger.LogInformation("List created [Id: {TodoListId}]", e.TodoListId);
                break;
            case TodoListItemAdded e:
                if (e.Item.Contains("Retry"))
                {
                    throw new RetryableException(e.Item);
                }

                if (e.Item.Contains("Fail"))
                {
                    throw new TerminalException(e.Item);
                }

                logger.LogInformation("Item added: {Item} [List: {TodoListId}]", e.Item, e.TodoListId);
                break;
            case TodoListItemCompleted e:
                logger.LogInformation("Item completed: {Item} [List: {TodoListId}]", e.Item, e.TodoListId);
                break;
        }

        return Task.CompletedTask;
    }
}
