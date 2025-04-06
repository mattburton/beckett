namespace TaskLists.Events;

public record TaskListNameChanged(Guid Id, string Name) : IInternalEvent;
