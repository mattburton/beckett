namespace MinimalApi.TodoList.AddingItems;

public class Configuration : IConfigureBeckett
{
    public void Configure(IServiceCollection services, BeckettOptions options)
    {
        options.Events.Map<TodoListItemAdded>("TodoListItemAdded");
    }
}
