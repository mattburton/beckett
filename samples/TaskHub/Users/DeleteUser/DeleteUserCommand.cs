using Users.Events;

namespace Users.DeleteUser;

public record DeleteUserCommand(string Username) : ICommand
{
    public IStreamName StreamName() => new UserStream(Username);

    public ExpectedVersion ExpectedVersion => ExpectedVersion.StreamExists;

    public IEnumerable<IInternalEvent> Execute()
    {
        yield return new UserDeleted(Username);
    }

    public IScenario[] Scenarios =>
    [
        new Scenario("user deleted")
            .Given()
            .When(new DeleteUserCommand(Example.String))
            .Then(new UserDeleted(Example.String))
    ];
}
