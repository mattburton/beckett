using System.Text.RegularExpressions;
using Taskmaster.TaskLists.AddTask;

namespace Taskmaster.TaskLists.Mentions;

public partial class MentionsHandler(IMessageStore messageStore) : IMessageHandler<TaskAdded>
{
    public async Task Handle(TaskAdded message, IMessageContext context, CancellationToken cancellationToken)
    {
        if (!message.Task.Contains('@'))
        {
            return;
        }

        var username = Username().Match(message.Task).Value.TrimStart('@');

        var followUpTask = message.Task.Replace("@", "");

        var item = $"Hi {username} - please follow up on {followUpTask}";

        await messageStore.AppendToStream(
            TaskList.StreamName(message.TaskListId),
            ExpectedVersion.StreamExists,
            message with { Task = item },
            cancellationToken
        );
    }

    [GeneratedRegex(@"(?<!\w)@(\w+)\b", RegexOptions.Compiled)]
    private static partial Regex Username();
}
