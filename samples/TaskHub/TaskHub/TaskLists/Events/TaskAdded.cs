namespace TaskHub.TaskLists.Events;

public record TaskAdded(Guid TaskListId, string Task) : IEvent;
