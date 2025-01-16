using TaskHub.Users.Contracts.Notifications;

namespace TaskHub.TaskLists.Slices.UserLookup;

[State]
public partial class UserLookupReadModel
{
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;

    private void Apply(UserAddedNotification message)
    {
        Username = message.Username;
        Email = message.Email;
    }
}
