using Contracts.TaskLists;
using Contracts.TaskLists.Commands;
using Contracts.TaskLists.Queries;

namespace TaskHub.TaskLists;

public class TaskListModule(IDispatcher dispatcher) : ITaskListModule
{
    public Task Execute(AddTaskList command, CancellationToken cancellationToken) =>
        dispatcher.Dispatch(command, cancellationToken);

    public Task Execute(AddTask command, CancellationToken cancellationToken) =>
        dispatcher.Dispatch(command, cancellationToken);

    public Task Execute(ChangeTaskListName command, CancellationToken cancellationToken) =>
        dispatcher.Dispatch(command, cancellationToken);

    public Task Execute(CompleteTask command, CancellationToken cancellationToken) =>
        dispatcher.Dispatch(command, cancellationToken);

    public Task Execute(DeleteTaskList command, CancellationToken cancellationToken) =>
        dispatcher.Dispatch(command, cancellationToken);

    public Task<GetTaskList.Result?> Execute(GetTaskList query, CancellationToken cancellationToken) =>
        dispatcher.Dispatch(query, cancellationToken);

    public Task<GetTaskLists.Result> Execute(GetTaskLists query, CancellationToken cancellationToken) =>
        dispatcher.Dispatch(query, cancellationToken);
}
