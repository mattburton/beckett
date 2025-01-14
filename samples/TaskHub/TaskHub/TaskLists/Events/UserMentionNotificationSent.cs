namespace TaskHub.TaskLists.Events;

public record UserMentionNotificationSent(Guid TaskListId, string Task, string Username);
