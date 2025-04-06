using Core.Contracts;
using Users.Events;
using Users.Contracts;

namespace Users.PublishEvent;

[State]
public partial class EventToPublishReadModel
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

    public IExternalEvent ToExternalEvent()
    {
        if (Deleted)
        {
            return new ExternalUserDeleted(UserName);
        }

        return new ExternalUserCreated(UserName, Email);
    }
}
