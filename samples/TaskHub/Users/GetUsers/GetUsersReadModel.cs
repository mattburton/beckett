using Users.Events;

namespace Users.GetUsers;

[State]
public partial class GetUsersReadModel : IStateView
{
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;

    private void Apply(UserRegistered message)
    {
        Username = message.Username;
        Email = message.Email;
    }

    public IScenario[] Scenarios =>
    [
        new Scenario("user registered")
            .Given(
                new UserRegistered(Example.String, Example.String)
            ).Then(
                new GetUsersReadModel
                {
                    Username = Example.String,
                    Email = Example.String
                }
            )
    ];
}
