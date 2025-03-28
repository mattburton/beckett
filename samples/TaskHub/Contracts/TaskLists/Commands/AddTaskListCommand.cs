namespace Contracts.TaskLists.Commands;

public record AddTaskListCommand(Guid Id, string Name) : ICommand;
