using TaskHub.Users.Events;

namespace TaskHub.Users.Slices.Users;

[ReadModel]
public partial class UsersReadModel
{
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;

    private void Apply(UserRegistered message)
    {
        Username = message.Username;
        Email = message.Email;
    }
}
