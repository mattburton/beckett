using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Streams;

namespace TaskHub.TaskLists.Processors.NotifyUser;

public partial record UserNotificationsToSend(Guid TaskListId) : IQuery<UserNotificationsToSend.State>
{
    public class Handler(
        IStreamReader reader
    ) : StreamStateQueryHandler<UserNotificationsToSend, State, State>(reader)
    {
        protected override IStreamName StreamName(UserNotificationsToSend query) =>
            new TaskListStream(query.TaskListId);

        protected override State Map(State state) => state;
    }

    [State]
    public partial class State : IHaveScenarios
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
                    new State
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
                    new State
                    {
                        Notifications = new Dictionary<string, bool>
                        {
                            { Example.String, true }
                        }
                    }
                )
        ];
    }
}
