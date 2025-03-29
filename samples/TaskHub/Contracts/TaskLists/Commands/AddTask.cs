namespace Contracts.TaskLists.Commands;

public record AddTask(Guid TaskListId, string Task) : ICommand;
