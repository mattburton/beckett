using TaskLists.Events;

namespace TaskLists.NotifyUser;

[State]
public partial class UserNotificationsToSendReadModel : IHaveScenarios
{
    private Dictionary<string, bool> Notifications { get; init; } = [];

    public bool AlreadySentFor(string task) => Notifications.TryGetValue(task, out var sent) && sent;

    private void Apply(UserMentionedInTask e)
    {
        Notifications[e.Task] = false;
    }

    private void Apply(UserNotificationSent e)
    {
        Notifications[e.Task] = true;
    }

    public IScenario[] Scenarios =>
    [
        new Scenario("user mentioned in task")
            .Given(new UserMentionedInTask(Example.Guid, Example.String, Example.String))
            .Then(
                new UserNotificationsToSendReadModel
                {
                    Notifications = new Dictionary<string, bool>
                    {
                        { Example.String, false }
                    }
                }
            ),
        new Scenario("user notification sent")
            .Given(
                new UserMentionedInTask(Example.Guid, Example.String, Example.String),
                new UserNotificationSent(Example.Guid, Example.String, Example.String)
            ).Then(
                new UserNotificationsToSendReadModel
                {
                    Notifications = new Dictionary<string, bool>
                    {
                        { Example.String, true }
                    }
                }
            )
    ];
}
