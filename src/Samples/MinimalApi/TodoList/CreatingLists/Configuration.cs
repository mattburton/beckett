namespace MinimalApi.TodoList.CreatingLists;

public static class Configuration
{
    public static IBeckettBuilder UseCreatingLists(this IBeckettBuilder builder)
    {
        builder.MapMessage<TodoListCreated>("TodoListCreated");

        return builder;
    }
}
