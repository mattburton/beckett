namespace TaskLists.Events;

public record TaskCompleted(Guid TaskListId, string Task) : IInternalEvent;
