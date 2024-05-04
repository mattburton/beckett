using MinimalApi.TodoList.AddingItems;
using MinimalApi.TodoList.CreatingLists;
using MinimalApi.TodoList.NotifyWhenItemAdded;
using MinimalApi.TodoList.NotifyWhenListCreated;

namespace MinimalApi.TodoList;

public static class Configuration
{
    public static IBeckettBuilder UseTodoListModule(this IBeckettBuilder builder) => builder
        .UseCreatingLists()
        .UseAddingItems()
        .UseNotifyWhenItemAdded()
        .UseNotifyWhenListCreated();
}
