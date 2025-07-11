using Microsoft.Extensions.Logging;
using TodoList.Events;

namespace TodoList.Wiretap;

/// <summary>
/// For demo purposes this subscription just logs each message that is appended so you can see activity as it occurs
/// </summary>
public class WiretapHandler
{
    public static void Handle(IMessageContext context, ILogger<WiretapHandler> logger)
    {
        var lag = DateTimeOffset.UtcNow.Subtract(context.Timestamp).TotalMilliseconds;

        switch (context.Message)
        {
            case TodoListCreated e:
                logger.LogInformation("List created [Id: {TodoListId}, Lag: {Lag}ms]", e.TodoListId, lag);
                break;
            case TodoListItemAdded e:
                if (e.Item.Contains("error"))
                {
                    throw new Exception("error");
                }

                logger.LogInformation("Item added: {Item} [List: {TodoListId}, Lag: {Lag}ms]", e.Item, e.TodoListId, lag);
                break;
            case TodoListItemCompleted e:
                logger.LogInformation("Item completed: {Item} [List: {TodoListId}, Lag: {Lag}ms]", e.Item, e.TodoListId, lag);
                break;
        }
    }
}
