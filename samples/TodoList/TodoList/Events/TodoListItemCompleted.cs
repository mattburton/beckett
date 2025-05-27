namespace TodoList.Events;

public record TodoListItemCompleted(Guid TodoListId, string Item);
