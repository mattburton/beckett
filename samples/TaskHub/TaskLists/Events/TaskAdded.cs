namespace TaskLists.Events;

public record TaskAdded(Guid TaskListId, string Task) : IInternalEvent;
