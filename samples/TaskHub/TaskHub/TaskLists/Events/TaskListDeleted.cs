namespace TaskHub.TaskLists.Events;

public record TaskListDeleted(Guid Id) : IEvent;
