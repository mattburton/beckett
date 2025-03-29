using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Streams;

namespace TaskHub.TaskLists.Processors.NotifyUser;

public record SendUserNotification(Guid TaskListId, string Task, string Username) : ICommand
{
    public class Handler : ICommandHandler<SendUserNotification>
    {
        public IStreamName StreamName(SendUserNotification command) =>
            new TaskListStream(command.TaskListId);

        public ExpectedVersion StreamVersion(SendUserNotification command) => ExpectedVersion.StreamExists;

        public IEnumerable<IEvent> Handle(SendUserNotification command)
        {
            yield return new UserNotificationSent(command.TaskListId, command.Task, command.Username);
        }

        public IScenario[] Scenarios =>
        [
            new Scenario("user notification sent")
                .Given()
                .When(new SendUserNotification(Example.Guid, Example.String, Example.String))
                .Then(new UserNotificationSent(Example.Guid, Example.String, Example.String))
        ];
    }
}
