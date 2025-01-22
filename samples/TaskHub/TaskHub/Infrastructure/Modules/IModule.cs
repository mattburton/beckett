namespace TaskHub.Infrastructure.Modules;

public interface IModule
{
    void MessageTypes(IMessageTypeBuilder builder);

    void Subscriptions(ISubscriptionBuilder builder);
}
