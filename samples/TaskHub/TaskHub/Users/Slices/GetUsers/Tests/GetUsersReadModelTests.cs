using TaskHub.Users.Events;

namespace TaskHub.Users.Slices.GetUsers.Tests;

public class GetUsersReadModelTests : StateSpecificationFixture<GetUsersReadModel>
{
    [Fact]
    public void user_registered()
    {
        var username = Generate.String();
        var email = Generate.String();

        Specification
            .Given(
                new UserRegistered(username, email)
            )
            .Then(
                new GetUsersReadModel
                {
                    Username = username,
                    Email = email
                }
            );
    }
}
