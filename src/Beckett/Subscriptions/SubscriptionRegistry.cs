using System.Collections.Concurrent;
using Beckett.Database;
using Npgsql;

namespace Beckett.Subscriptions;

public class SubscriptionRegistry(IPostgresDatabase database) : ISubscriptionRegistry
{
    private readonly ConcurrentDictionary<(string GroupName, string SubscriptionName), long> _subscriptionNameToId =
        new();

    private readonly ConcurrentDictionary<long, (string GroupName, string SubscriptionName)> _subscriptionIdToNames =
        new();

    public async Task Initialize(CancellationToken cancellationToken = default)
    {
        await LoadSubscriptionMappingsAsync(cancellationToken);
    }

    public long? GetSubscriptionId(string groupName, string subscriptionName)
    {
        return _subscriptionNameToId.TryGetValue((groupName, subscriptionName), out var id) ? id : null;
    }

    public (string GroupName, string SubscriptionName)? GetSubscription(long subscriptionId)
    {
        return _subscriptionIdToNames.TryGetValue(subscriptionId, out var names) ? names : null;
    }

    private async Task LoadSubscriptionMappingsAsync(CancellationToken cancellationToken)
    {
        await database.Execute(
            new LoadSubscriptionMappingsQuery(_subscriptionNameToId, _subscriptionIdToNames),
            cancellationToken
        );
    }

    private class LoadSubscriptionMappingsQuery(
        ConcurrentDictionary<(string GroupName, string SubscriptionName), long> subscriptionNameToId,
        ConcurrentDictionary<long, (string GroupName, string SubscriptionName)> subscriptionIdToNames)
        : IPostgresDatabaseQuery<int>
    {
        public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
        {
            const string sql = """
                                   SELECT s.id, s.name, sg.name as group_name
                                   FROM beckett.subscriptions s
                                   INNER JOIN beckett.subscription_groups sg ON s.subscription_group_id = sg.id
                               """;

            command.CommandText = sql;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var id = reader.GetFieldValue<long>(0);
                var name = reader.GetFieldValue<string>(1);
                var groupName = reader.GetFieldValue<string>(2);

                var key = (groupName, name);
                subscriptionNameToId.TryAdd(key, id);
                subscriptionIdToNames.TryAdd(id, key);
            }

            return 0;
        }
    }
}
