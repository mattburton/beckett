namespace TaskLists.Events;

public record TaskListDeleted(Guid Id) : IInternalEvent;
