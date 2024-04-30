namespace MinimalApi.TodoList.AddingItems;

public record TodoListItemAdded(Guid TodoListId, string Item);
