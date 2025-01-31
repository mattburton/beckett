using TaskHub.Users.Events;

namespace TaskHub.Users.Slices.DeleteUser;

public record DeleteUserCommand(string Username) : ICommand
{
    public string StreamName() => UserModule.StreamName(Username);

    public ExpectedVersion ExpectedVersion => ExpectedVersion.StreamExists;

    public IEnumerable<object> Execute()
    {
        yield return new UserDeleted(Username);
    }
}
