namespace TaskLists.Events;

public record UserMentionedInTask(Guid TaskListId, string Task, string Username) : IInternalEvent;
