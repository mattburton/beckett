namespace Contracts.TaskLists.Commands;

public record ChangeTaskListName(Guid Id, string Name) : ICommand;
