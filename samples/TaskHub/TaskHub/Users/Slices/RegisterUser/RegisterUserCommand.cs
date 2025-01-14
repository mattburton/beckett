using TaskHub.Users.Events;

namespace TaskHub.Users.Slices.RegisterUser;

public record RegisterUserCommand(string Username, string Email) : ICommand
{
    public IEnumerable<object> Execute()
    {
        yield return new UserRegistered(Username, Email);
    }
}
