using Contracts.TaskLists;
using Contracts.TaskLists.Commands;
using Contracts.TaskLists.Queries;

namespace TaskHub.TaskLists;

public class TaskListModule(
    ICommandDispatcher commandDispatcher,
    IQueryDispatcher queryDispatcher
) : ITaskListModule
{
    public Task Execute(AddTaskListCommand command, CancellationToken cancellationToken) =>
        commandDispatcher.Dispatch(command, cancellationToken);

    public Task Execute(AddTaskCommand command, CancellationToken cancellationToken) =>
        commandDispatcher.Dispatch(command, cancellationToken);

    public Task Execute(ChangeTaskListNameCommand command, CancellationToken cancellationToken) =>
        commandDispatcher.Dispatch(command, cancellationToken);

    public Task Execute(CompleteTaskCommand command, CancellationToken cancellationToken) =>
        commandDispatcher.Dispatch(command, cancellationToken);

    public Task Execute(DeleteTaskListCommand command, CancellationToken cancellationToken) =>
        commandDispatcher.Dispatch(command, cancellationToken);

    public Task<GetTaskListQuery.Result?> Execute(GetTaskListQuery query, CancellationToken cancellationToken) =>
        queryDispatcher.Dispatch(query, cancellationToken);

    public Task<GetTaskListsQuery.Result> Execute(GetTaskListsQuery query, CancellationToken cancellationToken) =>
        queryDispatcher.Dispatch(query, cancellationToken);
}
