using TaskHub.Infrastructure.Email;
using TaskHub.TaskLists.Events;

namespace TaskHub.TaskLists.Processors.NotifyUser;

public class NotifyUserProcessor(
    IDispatcher dispatcher,
    IEmailService emailService
) : IProcessor<UserMentionedInTask>
{
    public async Task<ProcessorResult> Handle(
        IMessageContext<UserMentionedInTask> context,
        CancellationToken cancellationToken
    )
    {
        var user = await dispatcher.Dispatch(new UserLookup(context.Message!.Username), cancellationToken);

        if (user == null)
        {
            return ProcessorResult.Empty;
        }

        var notificationsToSend = await dispatcher.Dispatch(
            new UserNotificationsToSend(context.Message.TaskListId),
            cancellationToken
        );

        if (notificationsToSend.AlreadySentFor(context.Message.Task))
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
            new SendUserNotification(context.Message.TaskListId, context.Message.Task, context.Message.Username)
        );

        return result;
    }
}
