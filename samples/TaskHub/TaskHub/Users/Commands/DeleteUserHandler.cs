using Contracts.Users.Commands;
using TaskHub.Users.Events;
using TaskHub.Users.Streams;

namespace TaskHub.Users.Commands;

public class DeleteUserHandler : ICommandHandler<DeleteUser>
{
    public IStreamName StreamName(DeleteUser command) => new UserStream(command.Username);

    public ExpectedVersion StreamVersion(DeleteUser command) => ExpectedVersion.StreamExists;

    public IEnumerable<IEvent> Handle(DeleteUser command)
    {
        yield return new UserDeleted(command.Username);
    }

    public IScenario[] Scenarios =>
    [
        new Scenario("user deleted")
            .Given()
            .When(new DeleteUser(Example.String))
            .Then(new UserDeleted(Example.String))
    ];
}
