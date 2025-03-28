namespace Contracts.TaskLists.Commands;

public record ChangeTaskListNameCommand(Guid Id, string Name) : ICommand;
