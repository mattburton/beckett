using TaskHub.Infrastructure.Email;
using TaskHub.TaskLists.Events;
using TaskHub.TaskLists.Slices.UserNotificationsToSend;
using TaskHub.Users.Contracts.Queries;

namespace TaskHub.TaskLists.Slices.UserNotificationV1;

public static class UserMentionedInTaskHandlerV1
{
    public static async Task Handle(
        UserMentionedInTask message,
        IQueryDispatcher queryDispatcher,
        IEmailService emailService,
        ICommandExecutor commandExecutor,
        CancellationToken cancellationToken
    )
    {
        if (!message.Task.Contains("V1"))
        {
            return;
        }

        var user = await queryDispatcher.Dispatch(new GetUserQuery(message.Username), cancellationToken);

        if (user == null)
        {
            return;
        }

        var notificationsToSend = await queryDispatcher.Dispatch(
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
                "User Mention Notification - V1",
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
