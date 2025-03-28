using Contracts.Users.Commands;
using Core.Streams;
using TaskHub.Users.Events;
using TaskHub.Users.Streams;

namespace TaskHub.Users.Commands;

public class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand>
{
    public IStreamName StreamName(RegisterUserCommand command) => new UserStream(command.Username);

    public ExpectedVersion StreamVersion(RegisterUserCommand command) => ExpectedVersion.StreamDoesNotExist;

    public IEnumerable<IEvent> Handle(RegisterUserCommand command)
    {
        yield return new UserRegistered(command.Username, command.Email);
    }
}
