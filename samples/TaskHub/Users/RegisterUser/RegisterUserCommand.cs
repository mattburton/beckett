using Users.Events;

namespace Users.RegisterUser;

public record RegisterUserCommand(string Username, string Email) : ICommand
{
    public IStreamName StreamName() => new UserStream(Username);

    public ExpectedVersion ExpectedVersion => ExpectedVersion.StreamDoesNotExist;

    public IEnumerable<IInternalEvent> Execute()
    {
        yield return new UserRegistered(Username, Email);
    }

    public IScenario[] Scenarios =>
    [
        new Scenario("user registered")
            .Given()
            .When(new RegisterUserCommand(Example.String, Example.String))
            .Then(new UserRegistered(Example.String, Example.String))
    ];
}
