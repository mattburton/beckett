using Core.Streams;
using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Streams;

namespace TaskHub.TaskLists.Processors.NotifyUser;

public record SendUserNotificationCommand(Guid TaskListId, string Task, string Username) : ICommand
{
    public class Handler : ICommandHandler<SendUserNotificationCommand>
    {
        public IStreamName StreamName(SendUserNotificationCommand command) =>
            new TaskListStream(command.TaskListId);

        public ExpectedVersion StreamVersion(SendUserNotificationCommand command) => ExpectedVersion.StreamExists;

        public IEnumerable<IEvent> Handle(SendUserNotificationCommand command)
        {
            yield return new UserNotificationSent(command.TaskListId, command.Task, command.Username);
        }
    }
}
