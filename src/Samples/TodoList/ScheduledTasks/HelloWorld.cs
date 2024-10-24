using Microsoft.Extensions.Logging;

namespace TodoList.ScheduledTasks;

public class HelloWorld
{
    public class Handler(ILogger<Handler> logger)
    {
        public Task Handle(HelloWorld message, CancellationToken cancellationToken)
        {
            logger.LogInformation("Hello World!");

            return Task.CompletedTask;
        }
    }
}
