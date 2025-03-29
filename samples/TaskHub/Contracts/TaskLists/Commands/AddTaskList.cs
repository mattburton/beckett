namespace Contracts.TaskLists.Commands;

public record AddTaskList(Guid Id, string Name) : ICommand;
