using Beckett;

namespace Core.Modules;

public interface IModule
{
    void MessageTypes(IMessageTypeBuilder builder);

    void Subscriptions(ISubscriptionBuilder builder);
}
