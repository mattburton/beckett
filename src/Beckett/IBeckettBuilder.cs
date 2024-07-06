using Beckett.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Beckett;

public interface IBeckettBuilder
{
    IConfiguration Configuration { get; }
    IHostEnvironment Environment { get; }
    IServiceCollection Services { get; }

    ISubscriptionBuilder AddSubscription(string name);

    IBeckettBuilder Build(Action<IBeckettBuilder> configure);

    void Map<TMessage>(string name);

    void ScheduleRecurringMessage<TMessage>(
        string name,
        string cronExpression,
        string streamName,
        TMessage message,
        Dictionary<string, object>? metadata = null
    ) where TMessage : notnull;
}
