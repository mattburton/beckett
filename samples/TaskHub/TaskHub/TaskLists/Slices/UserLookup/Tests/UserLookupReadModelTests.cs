using TaskHub.Users.Notifications;

namespace TaskHub.TaskLists.Slices.UserLookup.Tests;

public class UserLookupReadModelTests : StateSpecificationFixture<UserLookupReadModel>
{
    [Fact]
    public void user_notification()
    {
        var username = Generate.String();
        var email = Generate.String();

        Specification
            .Given(
                new UserNotification(Operation.Create, username, email)
            )
            .Then(
                new UserLookupReadModel
                {
                    Username = username,
                    Email = email
                }
            );
    }
}
