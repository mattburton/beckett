namespace Contracts.TaskLists.Commands;

public record DeleteTaskListCommand(Guid Id) : ICommand;
