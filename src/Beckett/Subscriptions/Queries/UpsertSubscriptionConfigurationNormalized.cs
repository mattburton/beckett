using Beckett.Database;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class UpsertSubscriptionConfigurationNormalized(
    string groupName,
    string subscriptionName,
    string? category,
    string? streamName,
    string[]? messageTypeNames,
    int priority,
    bool skipDuringReplay
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        using var batch = new NpgsqlBatch(command.Connection, command.Transaction);

        // Step 1: Ensure message types exist if provided
        if (messageTypeNames != null && messageTypeNames.Length > 0)
        {
            var ensureTypesCommand = new NpgsqlBatchCommand("""
                INSERT INTO beckett.message_types (name)
                SELECT DISTINCT unnest($1)
                ON CONFLICT (name) DO NOTHING;
                """);
            ensureTypesCommand.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Text });
            batch.BatchCommands.Add(ensureTypesCommand);
        }

        // Step 2: Update subscription configuration
        var updateSubscriptionCommand = new NpgsqlBatchCommand("""
            UPDATE beckett.subscriptions
            SET category = $3,
                stream_name = $4,
                priority = $5,
                skip_during_replay = $6
            FROM beckett.subscription_groups sg
            WHERE beckett.subscriptions.subscription_group_id = sg.id
            AND sg.name = $1
            AND beckett.subscriptions.name = $2;
            """);
        updateSubscriptionCommand.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        updateSubscriptionCommand.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        updateSubscriptionCommand.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text, IsNullable = true });
        updateSubscriptionCommand.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text, IsNullable = true });
        updateSubscriptionCommand.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Integer });
        updateSubscriptionCommand.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Boolean });
        batch.BatchCommands.Add(updateSubscriptionCommand);

        // Step 3: Clear existing subscription message types
        var clearTypesCommand = new NpgsqlBatchCommand("""
            DELETE FROM beckett.subscription_message_types 
            WHERE subscription_id IN (
                SELECT s.id 
                FROM beckett.subscriptions s
                INNER JOIN beckett.subscription_groups sg ON s.subscription_group_id = sg.id
                WHERE sg.name = $1 AND s.name = $2
            );
            """);
        clearTypesCommand.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        clearTypesCommand.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
        batch.BatchCommands.Add(clearTypesCommand);

        // Step 4: Insert new subscription message types if provided
        if (messageTypeNames != null && messageTypeNames.Length > 0)
        {
            var insertTypesCommand = new NpgsqlBatchCommand("""
                INSERT INTO beckett.subscription_message_types (subscription_id, message_type_id)
                SELECT s.id, mt.id
                FROM beckett.subscriptions s
                INNER JOIN beckett.subscription_groups sg ON s.subscription_group_id = sg.id
                CROSS JOIN unnest($3) AS type_name
                INNER JOIN beckett.message_types mt ON type_name = mt.name
                WHERE sg.name = $1 AND s.name = $2;
                """);
            insertTypesCommand.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
            insertTypesCommand.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text });
            insertTypesCommand.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Text });
            batch.BatchCommands.Add(insertTypesCommand);
        }

        // Set parameter values
        var commandIndex = 0;

        if (messageTypeNames != null && messageTypeNames.Length > 0)
        {
            batch.BatchCommands[commandIndex].Parameters[0].Value = messageTypeNames;
            commandIndex++;
        }

        batch.BatchCommands[commandIndex].Parameters[0].Value = groupName;
        batch.BatchCommands[commandIndex].Parameters[1].Value = subscriptionName;
        batch.BatchCommands[commandIndex].Parameters[2].Value = category ?? (object)DBNull.Value;
        batch.BatchCommands[commandIndex].Parameters[3].Value = streamName ?? (object)DBNull.Value;
        batch.BatchCommands[commandIndex].Parameters[4].Value = priority;
        batch.BatchCommands[commandIndex].Parameters[5].Value = skipDuringReplay;
        commandIndex++;

        batch.BatchCommands[commandIndex].Parameters[0].Value = groupName;
        batch.BatchCommands[commandIndex].Parameters[1].Value = subscriptionName;
        commandIndex++;

        if (messageTypeNames != null && messageTypeNames.Length > 0)
        {
            batch.BatchCommands[commandIndex].Parameters[0].Value = groupName;
            batch.BatchCommands[commandIndex].Parameters[1].Value = subscriptionName;
            batch.BatchCommands[commandIndex].Parameters[2].Value = messageTypeNames;
        }

        await batch.ExecuteNonQueryAsync(cancellationToken);

        return 1;
    }
}