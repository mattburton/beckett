using TodoList.Events;
using TodoList.Mentions;
using TodoList.Wiretap;

namespace TodoList;

public static class Configuration
{
    public static void WithTodoListMessageTypes(this IBeckettBuilder builder)
    {
        MessageTypes(builder);
    }

    public static void WithTodoListSubscriptions(this IBeckettBuilder builder)
    {
        MessageTypes(builder);
        Subscriptions(builder);
    }

    private static void MessageTypes(IBeckettBuilder builder)
    {
        builder.Map<TodoListItemAdded>("todo_list_item_added");
        builder.Map<TodoListItemCompleted>("todo_list_item_completed");
        builder.Map<TodoListCreated>("todo_list_created");
    }

    private static void Subscriptions(IBeckettBuilder builder)
    {
        var group = builder.MapGroup("TodoList");

        group.AddSubscription("TodoList:Mentions")
            .Message<TodoListItemAdded>()
            .Handler(MentionsHandler.Handle);

        group.AddSubscription("TodoList:Wiretap")
            .Category(TodoList.Category)
            .Handler(WiretapHandler.Handle)
            .StartingPosition(StartingPosition.Earliest);
    }
}
