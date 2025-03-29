namespace Contracts.TaskLists.Commands;

public record DeleteTaskList(Guid Id) : ICommand;
