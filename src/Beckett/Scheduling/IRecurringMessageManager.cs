namespace Beckett.Scheduling;

public interface IRecurringMessageManager
{
    Task Create(
        string name,
        string cronExpression,
        string streamName,
        Message message,
        CancellationToken cancellationToken
    );
}
