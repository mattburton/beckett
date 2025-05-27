namespace TodoList.Events;

public record TodoListItemAdded(Guid TodoListId, string Item);
