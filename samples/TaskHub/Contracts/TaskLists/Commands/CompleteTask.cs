namespace Contracts.TaskLists.Commands;

public record CompleteTask(Guid TaskListId, string Task) : ICommand;
