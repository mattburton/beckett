namespace TodoList;

public class TodoList
{
    public const string Category = nameof(TodoList);

    public static string StreamName(Guid id) => $"{Category}-{id}";
}
