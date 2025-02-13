using TaskHub.Users.Events;

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

        var state = stream.ProjectTo<UserState>();

        await notificationPublisher.Publish(
            context.StreamName,
            state.ToNotification(),
            cancellationToken
        );
    }

    [State]
    public partial class UserState
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

        public Notifications.User ToNotification()
        {
            return new Notifications.User(Operation, UserName, Email);
        }
    }
}
