namespace TaskHub.TaskList.Events;

public record TaskCompleted(Guid TaskListId, string Task);
