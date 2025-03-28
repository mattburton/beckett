using Contracts.Users.Notifications;
using TaskHub.TaskLists.Processors.NotifyUser;

namespace Tests.TaskLists.Processors.NotifyUser;

public class UserLookupReadModelTests : ReadModelFixture<UserLookupReadModel>
{
    [Fact]
    public void user_notification()
    {
        var username = Generate.String();
        var email = Generate.String();

        Specification
            .Given(
                new UserCreatedNotification(username, email)
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
