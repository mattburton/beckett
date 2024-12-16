using System.Text.RegularExpressions;
using TodoList.AddItem;

namespace TodoList.Mentions;

public partial class MentionsHandler(IMessageStore messageStore) : IMessageHandler<TodoListItemAdded>
{
    public async Task Handle(TodoListItemAdded message, IMessageContext context, CancellationToken cancellationToken)
    {
        if (!message.Item.Contains('@'))
        {
            return;
        }

        var username = Username().Match(message.Item).Value.TrimStart('@');

        var followUpItem = message.Item.Replace("@", "");

        var item = $"Hi {username} - please follow up on {followUpItem}";

        await messageStore.AppendToStream(
            TodoList.StreamName(message.TodoListId),
            ExpectedVersion.StreamExists,
            message with { Item = item },
            cancellationToken
        );
    }

    [GeneratedRegex(@"(?<!\w)@(\w+)\b", RegexOptions.Compiled)]
    private static partial Regex Username();
}
