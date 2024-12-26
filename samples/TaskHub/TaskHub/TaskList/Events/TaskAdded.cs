namespace TaskHub.TaskList.Events;

public record TaskAdded(Guid TaskListId, string Task);
