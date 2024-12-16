namespace TodoList.GetList;

public record GetTodoList(Guid Id)
{
    public async Task<TodoListView?> Execute(
        IMessageStore messageStore,
        CancellationToken cancellationToken
    )
    {
        var stream = await messageStore.ReadStream(TodoList.StreamName(Id), cancellationToken);

        return stream.IsEmpty ? null : stream.ProjectTo<TodoListView>();
    }
}
