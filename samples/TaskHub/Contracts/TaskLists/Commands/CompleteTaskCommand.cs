namespace Contracts.TaskLists.Commands;

public record CompleteTaskCommand(Guid TaskListId, string Task) : ICommand;
