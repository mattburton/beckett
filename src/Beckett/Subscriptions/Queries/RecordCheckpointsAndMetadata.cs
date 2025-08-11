using Beckett.Database;
using Beckett.Database.Types;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class RecordCheckpointsAndMetadata(
    CheckpointType[] checkpoints,
    StreamMetadataType[] streamMetadata,
    MessageMetadataType[] messageMetadata
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        await using var batch = new NpgsqlBatch(command.Connection, command.Transaction);

        // Step 1: Ensure all message types exist and get their IDs
        var uniqueMessageTypes = messageMetadata
            .Select(m => m.MessageTypeName)
            .Distinct()
            .ToArray();

        if (uniqueMessageTypes.Length > 0)
        {
            var ensureTypesCommand = new NpgsqlBatchCommand(
                """
                INSERT INTO beckett.message_types (name)
                SELECT DISTINCT unnest($1)
                ON CONFLICT (name) DO NOTHING;
                """
            );
            ensureTypesCommand.Parameters.Add(
                new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Text }
            );
            batch.BatchCommands.Add(ensureTypesCommand);
        }

        // Step 2: Record checkpoints (existing logic)
        var checkpointCommand = new NpgsqlBatchCommand(
            """
            WITH checkpoint_data AS (
                SELECT c.stream_position, c.subscription_id, c.stream_name, c.stream_version
                FROM unnest($1) c
            ),
            new_checkpoints AS (
                INSERT INTO beckett.checkpoints (stream_position, subscription_id, stream_name, updated_at)
                SELECT cd.stream_position, cd.subscription_id, cd.stream_name, now()
                FROM checkpoint_data cd
                ON CONFLICT (subscription_id, stream_name) DO NOTHING
                RETURNING id, stream_position, status, subscription_id
            ),
            all_checkpoints AS (
                SELECT c.id, c.stream_position, c.status, c.subscription_id, cd.stream_version
                FROM checkpoint_data cd
                INNER JOIN beckett.checkpoints c ON cd.subscription_id = c.subscription_id AND cd.stream_name = c.stream_name
            ),
            inserted_ready AS (
                INSERT INTO beckett.checkpoints_ready (id, process_at, subscription_group_name, target_stream_version)
                SELECT DISTINCT ac.id, now(), sg.name, ac.stream_version
                FROM all_checkpoints ac
                INNER JOIN beckett.subscriptions s ON ac.subscription_id = s.id
                INNER JOIN beckett.subscription_groups sg ON s.subscription_group_id = sg.id
                WHERE ac.status = 'active'
                AND ac.stream_version > ac.stream_position
                ON CONFLICT (id) DO UPDATE
                    SET process_at = now(),
                        target_stream_version = EXCLUDED.target_stream_version
                RETURNING id
            )
            SELECT pg_notify('beckett:checkpoints', ac.subscription_id::text)
            FROM all_checkpoints ac
            WHERE ac.status = 'active' AND ac.stream_version > ac.stream_position;
            """
        );
        checkpointCommand.Parameters.Add(new NpgsqlParameter { DataTypeName = DataTypeNames.CheckpointArray() });
        batch.BatchCommands.Add(checkpointCommand);

        // Step 3: Ensure all stream categories exist
        var uniqueCategories = streamMetadata
            .Select(sm => sm.Category)
            .Distinct()
            .ToArray();

        if (uniqueCategories.Length > 0)
        {
            var ensureCategoriesCommand = new NpgsqlBatchCommand(
                """
                INSERT INTO beckett.stream_categories (name)
                SELECT DISTINCT unnest($1)
                ON CONFLICT (name) DO NOTHING;
                """
            );
            ensureCategoriesCommand.Parameters.Add(
                new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Text }
            );
            batch.BatchCommands.Add(ensureCategoriesCommand);
        }

        // Step 4: Record stream metadata
        if (streamMetadata.Length > 0)
        {
            var streamMetadataCommand = new NpgsqlBatchCommand(
                """
                WITH stream_data AS (
                    SELECT sm.stream_name, sm.category,
                           MAX(sm.latest_position) as latest_position,
                           MAX(sm.latest_global_position) as latest_global_position,
                           SUM(sm.message_count) as message_count
                    FROM unnest($1) sm
                    GROUP BY sm.stream_name, sm.category
                ),
                stream_data_with_category_id AS (
                    SELECT sd.stream_name, sc.id as stream_category_id, sd.latest_position, sd.latest_global_position, sd.message_count
                    FROM stream_data sd
                    INNER JOIN beckett.stream_categories sc ON sd.category = sc.name
                )
                INSERT INTO beckett.stream_index
                    (stream_name, stream_category_id, latest_position, latest_global_position, message_count, last_updated_at)
                SELECT sd.stream_name, sd.stream_category_id, sd.latest_position, sd.latest_global_position, sd.message_count, now()
                FROM stream_data_with_category_id sd
                ON CONFLICT (stream_name) DO UPDATE SET
                    latest_position = GREATEST(beckett.stream_index.latest_position, EXCLUDED.latest_position),
                    latest_global_position = GREATEST(beckett.stream_index.latest_global_position, EXCLUDED.latest_global_position),
                    message_count = beckett.stream_index.message_count + EXCLUDED.message_count,
                    last_updated_at = now()
                WHERE EXCLUDED.latest_global_position > beckett.stream_index.latest_global_position;
                """
            );
            streamMetadataCommand.Parameters.Add(
                new NpgsqlParameter { DataTypeName = DataTypeNames.StreamIndexArray() }
            );
            batch.BatchCommands.Add(streamMetadataCommand);
        }

        // Step 5: Record message metadata with normalized message types
        if (messageMetadata.Length > 0)
        {
            var messageMetadataCommand = new NpgsqlBatchCommand(
                """
                WITH message_data AS (
                    SELECT mm.id, mm.global_position, mm.stream_name, mm.stream_position,
                           mm.message_type_name, mm.correlation_id, mm.tenant, mm.timestamp
                    FROM unnest($1) mm
                ),
                message_with_type_and_stream_ids AS (
                    SELECT md.id, md.global_position, md.stream_name, md.stream_position,
                           mt.id as message_type_id, si.id as stream_index_id,
                           md.correlation_id, t.id as tenant_id, md.timestamp
                    FROM message_data md
                    INNER JOIN beckett.message_types mt ON md.message_type_name = mt.name
                    INNER JOIN beckett.stream_index si ON md.stream_name = si.stream_name
                    LEFT JOIN beckett.tenants t ON md.tenant = t.name
                )
                INSERT INTO beckett.message_index
                    (id, global_position, stream_position, stream_index_id, message_type_id, tenant_id, correlation_id, timestamp)
                SELECT mwts.id, mwts.global_position, mwts.stream_position,
                       mwts.stream_index_id, mwts.message_type_id, mwts.tenant_id, mwts.correlation_id, mwts.timestamp
                FROM message_with_type_and_stream_ids mwts
                ON CONFLICT (global_position, id) DO NOTHING;
                """
            );
            messageMetadataCommand.Parameters.Add(
                new NpgsqlParameter { DataTypeName = DataTypeNames.MessageIndexArray() }
            );
            batch.BatchCommands.Add(messageMetadataCommand);
        }

        // Step 6: Update stream types with normalized message types
        if (messageMetadata.Length > 0)
        {
            var streamTypesCommand = new NpgsqlBatchCommand(
                """
                WITH message_data AS (
                    SELECT mm.stream_name, mm.message_type_name, mm.timestamp
                    FROM unnest($1) mm
                ),
                stream_type_data AS (
                    SELECT md.stream_name, mt.id as message_type_id, si.id as stream_index_id,
                           MAX(md.timestamp) as timestamp,
                           COUNT(*) as message_count
                    FROM message_data md
                    INNER JOIN beckett.message_types mt ON md.message_type_name = mt.name
                    INNER JOIN beckett.stream_index si ON md.stream_name = si.stream_name
                    GROUP BY md.stream_name, mt.id, si.id
                )
                INSERT INTO beckett.stream_message_types (stream_index_id, message_type_id, last_seen_at, message_count)
                SELECT std.stream_index_id, std.message_type_id, std.timestamp, std.message_count
                FROM stream_type_data std
                ON CONFLICT (stream_index_id, message_type_id) DO UPDATE SET
                    last_seen_at = GREATEST(beckett.stream_message_types.last_seen_at, EXCLUDED.last_seen_at),
                    message_count = beckett.stream_message_types.message_count + EXCLUDED.message_count;
                """
            );
            streamTypesCommand.Parameters.Add(
                new NpgsqlParameter { DataTypeName = DataTypeNames.MessageIndexArray() }
            );
            batch.BatchCommands.Add(streamTypesCommand);
        }

        // Step 7: Update tenants (categories are now handled in step 3)
        if (messageMetadata.Length > 0)
        {
            var tenantsCommand = new NpgsqlBatchCommand(
                """
                WITH tenants_data AS (
                    SELECT mm.tenant
                    FROM unnest($1) mm
                    WHERE mm.tenant IS NOT NULL
                    GROUP BY mm.tenant
                )
                INSERT INTO beckett.tenants (name)
                SELECT td.tenant
                FROM tenants_data td
                ON CONFLICT (name) DO NOTHING;
                """
            );
            tenantsCommand.Parameters.Add(new NpgsqlParameter { DataTypeName = DataTypeNames.MessageIndexArray() });
            batch.BatchCommands.Add(tenantsCommand);
        }

        // Set parameter values
        var commandIndex = 0;

        if (uniqueMessageTypes.Length > 0)
        {
            batch.BatchCommands[commandIndex].Parameters[0].Value = uniqueMessageTypes;
            commandIndex++;
        }

        batch.BatchCommands[commandIndex].Parameters[0].Value = checkpoints;
        commandIndex++;

        if (uniqueCategories.Length > 0)
        {
            batch.BatchCommands[commandIndex].Parameters[0].Value = uniqueCategories;
            commandIndex++;
        }

        if (streamMetadata.Length > 0)
        {
            batch.BatchCommands[commandIndex].Parameters[0].Value = streamMetadata;
            commandIndex++;
        }

        if (messageMetadata.Length > 0)
        {
            batch.BatchCommands[commandIndex].Parameters[0].Value = messageMetadata;
            commandIndex++;
            batch.BatchCommands[commandIndex].Parameters[0].Value = messageMetadata;
            commandIndex++;
        }

        if (messageMetadata.Length > 0)
        {
            batch.BatchCommands[commandIndex].Parameters[0].Value = messageMetadata;
        }

        await batch.ExecuteNonQueryAsync(cancellationToken);

        return checkpoints.Length;
    }
}
