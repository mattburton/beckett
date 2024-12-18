using Microsoft.Extensions.Logging;
using Taskmaster.TaskLists.AddTask;
using Taskmaster.TaskLists.CompleteTask;
using Taskmaster.TaskLists.CreateList;

namespace Taskmaster.TaskLists.Notifications;

public class NotificationHandler(ILogger<NotificationHandler> logger) : IMessageHandler
{
    public Task Handle(IMessageContext context, CancellationToken _)
    {
        var lag = DateTimeOffset.UtcNow.Subtract(context.Timestamp).TotalMilliseconds;

        switch (context.Message)
        {
            case TaskListCreated e:
                logger.LogInformation("List created [Id: {Id}, Lag: {Lag}ms]", e.Id, lag);
                break;
            case TaskAdded e:
                if (e.Task.Contains("Retry"))
                {
                    throw new RetryableException(e.Task);
                }

                if (e.Task.Contains("Fail"))
                {
                    throw new TerminalException(e.Task);
                }

                logger.LogInformation("Task added: {Item} [List: {TaskListId}, Lag: {Lag}ms]", e.Task, e.TaskListId, lag);
                break;
            case TaskCompleted e:
                logger.LogInformation("Task completed: {Item} [List: {TaskListId}, Lag: {Lag}ms]", e.Item, e.TaskListId, lag);
                break;
        }

        return Task.CompletedTask;
    }
}
