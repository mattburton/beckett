namespace Beckett.Scheduling;

public record ScheduledMessageContext(string StreamName, Message Message);
