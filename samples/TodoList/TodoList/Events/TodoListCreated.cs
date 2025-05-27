namespace TodoList.Events;

public record TodoListCreated(Guid TodoListId, string Name);
