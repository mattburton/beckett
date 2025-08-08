namespace Beckett.Scheduling;

public class RecurringMessage(string name)
{
    internal string Name { get; } = name;
    internal string CronExpression { get; set; } = null!;
    internal string StreamName { get; set; } = null!;
    internal Message Message { get; set; } = null!;
}
