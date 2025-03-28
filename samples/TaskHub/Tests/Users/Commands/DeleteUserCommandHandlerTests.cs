using Contracts.Users.Commands;
using TaskHub.Users.Commands;
using TaskHub.Users.Events;

namespace Tests.Users.Commands;

public class DeleteUserCommandHandlerTests : CommandHandlerFixture<DeleteUserCommand, DeleteUserCommandHandler>
{
    [Fact]
    public void user_deleted()
    {
        var username = Generate.String();

        Specification
            .When(
                new DeleteUserCommand(username)
            )
            .Then(
                new UserDeleted(username)
            );
    }
}
