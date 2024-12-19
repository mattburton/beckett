using System.Text.RegularExpressions;
using Beckett.Commands;
using Taskmaster.TaskLists.AddTask;

namespace Taskmaster.TaskLists.AssignReviewTask;

public partial class TaskAddedHandler(ICommandInvoker commandInvoker) : IMessageHandler<TaskAdded>
{
    public async Task Handle(TaskAdded message, IMessageContext context, CancellationToken cancellationToken)
    {
        var match = Username().Match(message.Task);

        if (!match.Success)
        {
            return;
        }

        var username = match.Value.TrimStart('@');

        var followUpTask = message.Task.Replace("@", "");

        var task = $"{username} - review required: {followUpTask}";

        await commandInvoker.Execute(
            TaskList.StreamName(message.TaskListId),
            new AddTaskCommand(Guid.NewGuid(), task),
            cancellationToken
        );
    }

    [GeneratedRegex(@"(?<!\w)@(\w+)\b", RegexOptions.Compiled)]
    private static partial Regex Username();
}
