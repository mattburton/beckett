namespace MinimalApi.TodoList.AddingItems;

public static class Configuration
{
    public static IBeckettBuilder UseAddingItems(this IBeckettBuilder builder)
    {
        builder.MapEvent<TodoListItemAdded>("TodoListItemAdded");

        return builder;
    }
}
