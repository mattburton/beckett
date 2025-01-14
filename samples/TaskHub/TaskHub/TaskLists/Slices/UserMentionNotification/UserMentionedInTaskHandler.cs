using TaskHub.Infrastructure.Email;
using TaskHub.TaskLists.Events;
using TaskHub.Users.Slices.GetUser;

namespace TaskHub.TaskLists.Slices.UserMentionNotification;

public static class UserMentionedInTaskHandler
{
    public static async Task Handle(
        UserMentionedInTask message,
        IQueryExecutor queryExecutor,
        IEmailService emailService,
        ICommandExecutor commandExecutor,
        CancellationToken cancellationToken
    )
    {
        var user = await queryExecutor.Execute(new GetUserQuery(message.Username), cancellationToken);

        if (user == null)
        {
            return;
        }

        var notificationToSend = await queryExecutor.Execute(
            new NotificationToSendQuery(message.TaskListId),
            cancellationToken
        );

        if (notificationToSend == null || notificationToSend.AlreadySentFor(message.Task))
        {
            return;
        }

        await emailService.Send(
            new EmailMessage(
                "notifications@taskhub.com",
                user.Email,
                "TaskHub Notification",
                $"You were mentioned in a task: {message.Task}"
            ),
            cancellationToken
        );

        await commandExecutor.Execute(
            TaskListModule.StreamName(message.TaskListId),
            new SendUserMentionNotificationCommand(message.TaskListId, message.Task, message.Username),
            cancellationToken
        );
    }
}
