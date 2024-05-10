using System.Text.RegularExpressions;
using TodoList.AddItem;

namespace TodoList.Mentions;

public partial class MentionsHandler(IMessageStore messageStore)
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

        await messageStore.AppendToStream(
            StreamName.For<TodoList>(e.TodoListId),
            ExpectedVersion.StreamExists,
            e with { Item = item },
            cancellationToken
        );
    }

    [GeneratedRegex(@"(?<!\w)@(\w+)\b", RegexOptions.Compiled)]
    private static partial Regex Username();
}
