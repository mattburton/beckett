namespace MinimalApi.TodoList.CreatingLists;

public class Configuration : IConfigureBeckett
{
    public void Configure(IServiceCollection services, BeckettOptions options)
    {
        options.Events.Map<TodoListCreated>("TodoListCreated");
    }
}
