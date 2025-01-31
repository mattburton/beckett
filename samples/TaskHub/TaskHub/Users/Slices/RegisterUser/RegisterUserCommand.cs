using TaskHub.Users.Events;

namespace TaskHub.Users.Slices.RegisterUser;

public record RegisterUserCommand(string Username, string Email) : ICommand
{
    public string StreamName() => UserModule.StreamName(Username);

    public ExpectedVersion ExpectedVersion => ExpectedVersion.StreamDoesNotExist;

    public IEnumerable<object> Execute()
    {
        yield return new UserRegistered(Username, Email);
    }
}
