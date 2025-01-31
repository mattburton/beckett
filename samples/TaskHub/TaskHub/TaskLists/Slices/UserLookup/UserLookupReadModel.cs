using TaskHub.Users.Notifications;

namespace TaskHub.TaskLists.Slices.UserLookup;

[State]
public partial class UserLookupReadModel
{
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;

    private void Apply(UserChanged message)
    {
        Username = message.Username;
        Email = message.Email;
    }
}
