using System.Collections.Concurrent;

namespace Beckett.Scheduling;

public static class RecurringMessageRegistry
{
    private static readonly ConcurrentDictionary<string, RecurringMessage> RecurringMessages = new();

    public static bool TryAdd(string name, out RecurringMessage recurringMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (RecurringMessages.ContainsKey(name))
        {
            recurringMessage = null!;

            return false;
        }

        recurringMessage = new RecurringMessage(name);

        RecurringMessages.TryAdd(name, recurringMessage);

        return true;
    }

    public static IEnumerable<RecurringMessage> All() => RecurringMessages.Values;

    public static bool Any() => RecurringMessages.Count > 0;
}
