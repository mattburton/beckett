using Contracts.Users.Commands;
using TaskHub.Users.Events;
using TaskHub.Users.Streams;

namespace TaskHub.Users.Commands;

public class RegisterUserHandler : ICommandHandler<RegisterUser>
{
    public IStreamName StreamName(RegisterUser command) => new UserStream(command.Username);

    public ExpectedVersion StreamVersion(RegisterUser command) => ExpectedVersion.StreamDoesNotExist;

    public IEnumerable<IEvent> Handle(RegisterUser command)
    {
        yield return new UserRegistered(command.Username, command.Email);
    }

    public IScenario[] Scenarios =>
    [
        new Scenario("user registered")
            .Given()
            .When(new RegisterUser(Example.String, Example.String))
            .Then(new UserRegistered(Example.String, Example.String))
    ];
}
