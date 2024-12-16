using Microsoft.Extensions.Logging;
using TodoList.AddItem;
using TodoList.CompleteItem;
using TodoList.CreateList;

namespace TodoList.Notifications;

public class NotificationHandler(ILogger<NotificationHandler> logger) : IMessageHandler
{
    public Task Handle(IMessageContext context, CancellationToken _)
    {
        var lag = DateTimeOffset.UtcNow.Subtract(context.Timestamp).TotalMilliseconds;

        switch (context.Message)
        {
            case TodoListCreated e:
                logger.LogInformation("List created [Id: {TodoListId}, Lag: {Lag}ms]", e.TodoListId, lag);
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

                logger.LogInformation("Item added: {Item} [List: {TodoListId}, Lag: {Lag}ms]", e.Item, e.TodoListId, lag);
                break;
            case TodoListItemCompleted e:
                logger.LogInformation("Item completed: {Item} [List: {TodoListId}, Lag: {Lag}ms]", e.Item, e.TodoListId, lag);
                break;
        }

        return Task.CompletedTask;
    }
}
