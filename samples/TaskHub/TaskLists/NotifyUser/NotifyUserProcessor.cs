using Infrastructure.Email;
using TaskLists.Events;

namespace TaskLists.NotifyUser;

public class NotifyUserProcessor(
    ITaskListModule module,
    IEmailService emailService
) : IProcessor<UserMentionedInTask>
{
    public async Task<ProcessorResult> Handle(
        IMessageContext<UserMentionedInTask> context,
        CancellationToken cancellationToken
    )
    {
        var user = await module.Execute(new UserLookupQuery(context.Message!.Username), cancellationToken);

        if (user == null)
        {
            return ProcessorResult.Empty;
        }

        var notificationsToSend = await module.Execute(
            new UserNotificationsToSendQuery(context.Message.TaskListId),
            cancellationToken
        );

        if (notificationsToSend?.AlreadySentFor(context.Message.Task) ?? false)
        {
            return ProcessorResult.Empty;
        }

        await emailService.Send(
            new EmailMessage(
                "notifications@taskhub.com",
                user.Email,
                "User Notification",
                $"You were mentioned in a task: {context.Message.Task}"
            ),
            cancellationToken
        );

        var result = new ProcessorResult();

        result.Execute(
            new SendUserNotificationCommand(context.Message.TaskListId, context.Message.Task, context.Message.Username)
        );

        return result;
    }
}
