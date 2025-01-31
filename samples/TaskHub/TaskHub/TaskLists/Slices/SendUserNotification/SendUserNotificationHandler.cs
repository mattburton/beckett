using TaskHub.Infrastructure.Email;
using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Slices.UserLookup;
using TaskHub.TaskLists.Slices.UserNotificationsToSend;

namespace TaskHub.TaskLists.Slices.SendUserNotification;

public static class SendUserNotificationHandler
{
    public static async Task Handle(
        UserMentionedInTask message,
        IQueryBus queryBus,
        IEmailService emailService,
        ICommandBus commandBus,
        CancellationToken cancellationToken
    )
    {
        var user = await queryBus.Send(new UserLookupQuery(message.Username), cancellationToken);

        if (user == null)
        {
            return;
        }

        var notificationsToSend = await queryBus.Send(
            new UserNotificationsToSendQuery(message.TaskListId),
            cancellationToken
        );

        if (notificationsToSend == null || notificationsToSend.AlreadySentFor(message.Task))
        {
            return;
        }

        await emailService.Send(
            new EmailMessage(
                "notifications@taskhub.com",
                user.Email,
                "User Notification",
                $"You were mentioned in a task: {message.Task}"
            ),
            cancellationToken
        );

        await commandBus.Send(
            new SendUserNotificationCommand(message.TaskListId, message.Task, message.Username),
            cancellationToken
        );
    }
}
