namespace TodoList.GetList;

public record GetTodoList(Guid id)
{
    public async Task<TodoListView?> Execute(
        IMessageStore messageStore,
        CancellationToken cancellationToken
    )
    {
        var stream = await messageStore.ReadStream(TodoList.StreamName(id), cancellationToken);

        return stream.IsEmpty ? null : stream.ProjectTo<TodoListView>();
    }
}
