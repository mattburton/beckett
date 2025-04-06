using TaskLists.Events;

namespace TaskLists.NotifyUser;

public record SendUserNotificationCommand(Guid TaskListId, string Task, string Username) : ICommand
{
    public IStreamName StreamName() => new TaskListStream(TaskListId);

    public ExpectedVersion ExpectedVersion => ExpectedVersion.StreamExists;

    public IEnumerable<IInternalEvent> Execute()
    {
        yield return new UserNotificationSent(TaskListId, Task, Username);
    }

    public IScenario[] Scenarios =>
    [
        new Scenario("user notification sent")
            .Given()
            .When(new SendUserNotificationCommand(Example.Guid, Example.String, Example.String))
            .Then(new UserNotificationSent(Example.Guid, Example.String, Example.String))
    ];
}
