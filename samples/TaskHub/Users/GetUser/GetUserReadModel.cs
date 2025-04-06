using Users.Events;

namespace Users.GetUser;

[State]
public partial class GetUserReadModel : IHaveScenarios
{
    public string Email { get; set; } = null!;
    public string Username { get; set; } = null!;
    public bool Deleted { get; set; }

    private void Apply(UserRegistered e)
    {
        Username = e.Username;
        Email = e.Email;
    }

    private void Apply(UserDeleted _) => Deleted = true;

    public IScenario[] Scenarios =>
    [
        new Scenario("user registered")
            .Given(
                new UserRegistered(Example.String, Example.String)
            ).Then(
                new GetUserReadModel
                {
                    Username = Example.String,
                    Email = Example.String,
                    Deleted = false
                }
            ),
        new Scenario("user deleted")
            .Given(
                new UserRegistered(Example.String, Example.String),
                new UserDeleted(Example.String)
            ).Then(
                new GetUserReadModel
                {
                    Username = Example.String,
                    Email = Example.String,
                    Deleted = true
                }
            )
    ];
}
