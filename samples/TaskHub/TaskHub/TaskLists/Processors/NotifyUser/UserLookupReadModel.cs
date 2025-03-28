using Contracts.Users.Notifications;

namespace TaskHub.TaskLists.Processors.NotifyUser;

[ReadModel]
public partial class UserLookupReadModel
{
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;

    private void Apply(UserCreatedNotification message)
    {
        Username = message.Username;
        Email = message.Email;
    }
}
