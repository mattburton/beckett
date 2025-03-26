using TaskHub.Users.Events;
using TaskHub.Users.Notifications;

namespace TaskHub.Users.Slices.PublishNotification;

public static partial class UserNotificationPublisher
{
    public static async Task Handle(
        IMessageContext context,
        IMessageStore messageStore,
        INotificationPublisher notificationPublisher,
        CancellationToken cancellationToken
    )
    {
        var stream = await messageStore.ReadStream(context.StreamName, cancellationToken);

        var model = stream.ProjectTo<Model>();

        await notificationPublisher.Publish(
            context.StreamName,
            model.ToNotification(),
            cancellationToken
        );
    }

    [ReadModel]
    public partial class Model
    {
        private Operation Operation { get; set; }
        private string UserName { get; set; } = null!;
        private string Email { get; set; } = null!;

        private void Apply(UserRegistered m)
        {
            Operation = Operation.Create;
            UserName = m.Username;
            Email = m.Email;
        }

        private void Apply(UserDeleted _)
        {
            Operation = Operation.Delete;
        }

        public UserNotification ToNotification()
        {
            return new UserNotification(Operation, UserName, Email);
        }
    }
}
