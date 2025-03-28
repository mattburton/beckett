using Contracts.TaskLists.Commands;
using Contracts.TaskLists.Exceptions;
using Contracts.TaskLists.Queries;

namespace Contracts.TaskLists;

public interface ITaskListModule : IModule
{
    /// <summary>
    /// Add a task list
    /// </summary>
    /// <param name="command"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="TaskListNameInUseException">Another task list with the same name has already been added</exception>
    /// <exception cref="ResourceAlreadyExistsException">Another task list with the same ID has already been added</exception>
    Task Execute(AddTaskListCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Add task to list
    /// </summary>
    /// <param name="command"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="TaskAlreadyAddedException">Another task with the same name has already been added to the list</exception>
    Task Execute(AddTaskCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Change task list name
    /// </summary>
    /// <param name="command"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="ResourceNotFoundException">The task list does not exist</exception>
    Task Execute(ChangeTaskListNameCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Mark task as complete
    /// </summary>
    /// <param name="command"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="TaskAlreadyCompletedException">The task has already been marked as completed</exception>
    Task Execute(CompleteTaskCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Delete task list
    /// </summary>
    /// <param name="command"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="ResourceNotFoundException">The task list does not exist</exception>
    Task Execute(DeleteTaskListCommand command, CancellationToken cancellationToken);

    /// <summary>
    /// Get task list by ID
    /// </summary>
    /// <param name="query"></param>
    /// <param name="cancellationToken"></param>
    Task<GetTaskListQuery.Result?> Execute(GetTaskListQuery query, CancellationToken cancellationToken);

    /// <summary>
    /// Get task lists
    /// </summary>
    /// <param name="query"></param>
    /// <param name="cancellationToken"></param>
    Task<GetTaskListsQuery.Result> Execute(GetTaskListsQuery query, CancellationToken cancellationToken);
}
