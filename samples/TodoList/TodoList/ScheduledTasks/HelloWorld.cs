using Microsoft.Extensions.Logging;

namespace TodoList.ScheduledTasks;

public record HelloWorld
{
    public static void Handle(HelloWorld message, ILogger<HelloWorld> logger, CancellationToken cancellationToken)
    {
        logger.LogInformation("Hello World!");
    }
}
