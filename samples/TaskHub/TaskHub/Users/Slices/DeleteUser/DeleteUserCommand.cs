using TaskHub.Users.Events;

namespace TaskHub.Users.Slices.DeleteUser;

public record DeleteUserCommand(string Username) : ICommand
{
    public IEnumerable<object> Execute()
    {
        yield return new UserDeleted(Username);
    }
}
