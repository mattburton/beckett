using TaskHub.Users.Events;
using TaskHub.Users.Slices.DeleteUser;

namespace Tests.Users.Slices.DeleteUser;

public class DeleteUserCommandTests : CommandFixture<DeleteUserCommand>
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
