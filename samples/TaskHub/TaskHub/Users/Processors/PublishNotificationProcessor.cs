using Contracts.Users.Notifications;
using TaskHub.Users.Events;

namespace TaskHub.Users.Processors;

public partial class PublishNotificationProcessor(IStreamReader reader) : IProcessor
{
    public async Task<ProcessorResult> Handle(IMessageContext context, CancellationToken cancellationToken)
    {
        var stream = await reader.ReadStream(context.StreamName, cancellationToken);

        var model = stream.ProjectTo<State>();

        var result = new ProcessorResult();

        result.Publish(model.ToNotification());

        return result;
    }

    [State]
    public partial class State
    {
        private string UserName { get; set; } = null!;
        private string Email { get; set; } = null!;
        private bool Deleted { get; set; }

        private void Apply(UserRegistered m)
        {
            UserName = m.Username;
            Email = m.Email;
        }

        private void Apply(UserDeleted _)
        {
            Deleted = true;
        }

        public INotification ToNotification()
        {
            if (Deleted)
            {
                return new UserDeletedNotification(UserName);
            }

            return new UserCreatedNotification(UserName, Email);
        }
    }
}
