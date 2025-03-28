namespace Contracts.TaskLists.Commands;

public record AddTaskCommand(Guid TaskListId, string Task) : ICommand;
