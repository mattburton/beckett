namespace TaskLists.Events;

public record TaskListAdded(Guid Id, string Name) : IInternalEvent;
