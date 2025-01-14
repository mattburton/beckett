using TaskHub.Users.Events;

namespace TaskHub.Users.Slices.DeleteUser.Tests;

public class DeleteUserCommandTests : CommandSpecificationFixture<DeleteUserCommand>
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
