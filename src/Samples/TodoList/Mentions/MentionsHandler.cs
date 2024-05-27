using System.Text.RegularExpressions;
using TodoList.AddItem;

namespace TodoList.Mentions;

public partial class MentionsHandler(IMessageSession messageSession)
{
    public async Task Handle(TodoListItemAdded e, CancellationToken cancellationToken)
    {
        if (!e.Item.Contains('@'))
        {
            return;
        }

        var username = Username().Match(e.Item).Value.TrimStart('@');

        var followUpItem = e.Item.Replace("@", "");

        var item = $"Hi {username} - please follow up on {followUpItem}";

        messageSession.AppendToStream(
            TodoList.StreamName(e.TodoListId),
            ExpectedVersion.StreamExists,
            e with { Item = item }
        );

        messageSession.AppendToStream(
            TodoList.StreamName(e.TodoListId),
            ExpectedVersion.StreamExists,
            new FollowUpItemAdded(e.TodoListId, e.Item, item)
        );

        await messageSession.SaveChanges(cancellationToken);
    }

    [GeneratedRegex(@"(?<!\w)@(\w+)\b", RegexOptions.Compiled)]
    private static partial Regex Username();
}
