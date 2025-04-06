namespace TaskLists.Events;

public record UserNotificationSent(Guid TaskListId, string Task, string Username) : IInternalEvent;
