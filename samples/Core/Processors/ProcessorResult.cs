using Core.Contracts;

namespace Core.Processors;

public class ProcessorResult
{
    private readonly List<INotification> _notifications = [];
    private readonly List<IJob> _jobs = [];

    public ICommand? Command { get; private set; }
    public IReadOnlyList<INotification> Notifications => _notifications;
    public IReadOnlyList<IJob> Jobs => _jobs;

    public static ProcessorResult Empty { get; } = new();

    /// <summary>
    /// Enqueue a job to be executed asynchronously.
    /// </summary>
    /// <param name="job">Job to enqueue</param>
    public void Enqueue(IJob job)
    {
        _jobs.Add(job);
    }

    /// <summary>
    /// Execute a command immediately when processing the result. If the command fails then the handler that executed it
    /// will be retried, with the command being executed again as a result.
    /// </summary>
    /// <param name="command">Command to enqueue</param>
    public void Execute(ICommand command)
    {
        Command = command;
    }
    
    /// <summary>
    /// Publish a notification
    /// </summary>
    /// <param name="notification">Notification to publish</param>
    public void Publish(INotification notification)
    {
        _notifications.Add(notification);
    }

    /// <summary>
    /// Schedule a job to be executed asynchronously after a specified delay.
    /// </summary>
    /// <param name="job">Job to schedule</param>
    /// <param name="delay">Delay until the job should be executed</param>
    public void Schedule(IJob job, TimeSpan delay)
    {
        _jobs.Add(new ScheduledJob(job, delay));
    }

    public static implicit operator Task<ProcessorResult>(ProcessorResult instance) => Task.FromResult(instance);
}
