namespace Beckett.Scheduling;

public interface IRecurringMessageRegistry
{
    bool TryAdd(string name, out RecurringMessage recurringMessage);

    IEnumerable<RecurringMessage> All();

    bool Any();
}

public class RecurringMessage(string name)
{
    internal string Name { get; } = name;
    internal string CronExpression { get; set; } = null!;
    internal string StreamName { get; set; } = null!;
    internal object Message { get; set; } = null!;
    internal Dictionary<string, object> Metadata { get; set; } = null!;
}

public class RecurringMessageRegistry : IRecurringMessageRegistry
{
    private readonly Dictionary<string, RecurringMessage> _recurringMessages = new();

    public bool TryAdd(string name, out RecurringMessage recurringMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (_recurringMessages.ContainsKey(name))
        {
            recurringMessage = null!;

            return false;
        }

        recurringMessage = new RecurringMessage(name);

        _recurringMessages.Add(name, recurringMessage);

        return true;
    }

    public IEnumerable<RecurringMessage> All() => _recurringMessages.Values;

    public bool Any() => _recurringMessages.Count > 0;
}
