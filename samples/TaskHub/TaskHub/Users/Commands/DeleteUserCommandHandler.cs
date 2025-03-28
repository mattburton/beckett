using Contracts.Users.Commands;
using Core.Streams;
using TaskHub.Users.Events;
using TaskHub.Users.Streams;

namespace TaskHub.Users.Commands;

public class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand>
{
    public IStreamName StreamName(DeleteUserCommand command) => new UserStream(command.Username);

    public ExpectedVersion StreamVersion(DeleteUserCommand command) => ExpectedVersion.StreamExists;

    public IEnumerable<IEvent> Handle(DeleteUserCommand command)
    {
        yield return new UserDeleted(command.Username);
    }
}
