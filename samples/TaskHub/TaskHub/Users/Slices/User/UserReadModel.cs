using TaskHub.Users.Events;

namespace TaskHub.Users.Slices.User;

[State]
public partial class UserReadModel
{
    public string Email { get; set; } = null!;
    public string Username { get; set; } = null!;
    public bool Deleted { get; set; }

    private void Apply(UserRegistered e)
    {
        Username = e.Username;
        Email = e.Email;
    }

    private void Apply(UserDeleted _) => Deleted = true;
}
