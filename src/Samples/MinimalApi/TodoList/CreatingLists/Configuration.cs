namespace MinimalApi.TodoList.CreatingLists;

public static class Configuration
{
    public static IBeckettBuilder UseCreatingLists(this IBeckettBuilder builder)
    {
        builder.MapEvent<TodoListCreated>("TodoListCreated");

        return builder;
    }
}
