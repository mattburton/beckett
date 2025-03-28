namespace TaskHub.TaskLists.Events;

public record TaskCompleted(Guid TaskListId, string Task) : IEvent;
