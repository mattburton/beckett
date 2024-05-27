namespace TodoList.Mentions;

public record FollowUpItemAdded(Guid TodoListId, string OriginalItem, string FollowUpItem);
