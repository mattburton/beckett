namespace TodoList.ScheduledTasks;

public class HelloWorld
{
    public Task Handle(HelloWorld message, CancellationToken cancellationToken)
    {
        Console.WriteLine("HELLO WORLD");

        return Task.CompletedTask;
    }
}
