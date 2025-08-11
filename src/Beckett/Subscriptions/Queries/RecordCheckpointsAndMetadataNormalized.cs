using Beckett.Database;
using Beckett.Database.Types;
using Npgsql;
using NpgsqlTypes;

namespace Beckett.Subscriptions.Queries;

public class RecordCheckpointsAndMetadataNormalized(
    CheckpointType[] checkpoints,
    StreamMetadataType[] streamMetadata,
    MessageMetadataType[] messageMetadata
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        using var batch = new NpgsqlBatch(command.Connection, command.Transaction);

        // Step 1: Ensure all message types exist and get their IDs
        var uniqueMessageTypes = messageMetadata
            .Select(m => m.MessageTypeName)
            .Distinct()
            .ToArray();

        if (uniqueMessageTypes.Length > 0)
        {
            var ensureTypesCommand = new NpgsqlBatchCommand("""
                INSERT INTO beckett.message_types (name)
                SELECT DISTINCT unnest($1)
                ON CONFLICT (name) DO NOTHING;
                """);
            ensureTypesCommand.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Text });
            batch.BatchCommands.Add(ensureTypesCommand);
        }

        // Step 2: Record checkpoints (existing logic)
        var checkpointCommand = new NpgsqlBatchCommand("""
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
            """);
        checkpointCommand.Parameters.Add(new NpgsqlParameter { DataTypeName = DataTypeNames.CheckpointArray() });
        batch.BatchCommands.Add(checkpointCommand);

        // Step 3: Record stream metadata
        if (streamMetadata.Length > 0)
        {
            var streamMetadataCommand = new NpgsqlBatchCommand("""
                WITH stream_data AS (
                    SELECT sm.stream_name, sm.category, 
                           MAX(sm.latest_position) as latest_position,
                           MAX(sm.latest_global_position) as latest_global_position,
                           SUM(sm.message_count) as message_count
                    FROM unnest($1) sm
                    GROUP BY sm.stream_name, sm.category
                )
                INSERT INTO beckett.stream_metadata 
                    (stream_name, category, latest_position, latest_global_position, message_count, last_updated_at)
                SELECT sd.stream_name, sd.category, sd.latest_position, sd.latest_global_position, sd.message_count, now()
                FROM stream_data sd
                ON CONFLICT (stream_name) DO UPDATE SET
                    latest_position = GREATEST(beckett.stream_metadata.latest_position, EXCLUDED.latest_position),
                    latest_global_position = GREATEST(beckett.stream_metadata.latest_global_position, EXCLUDED.latest_global_position),
                    message_count = beckett.stream_metadata.message_count + EXCLUDED.message_count,
                    last_updated_at = now()
                WHERE EXCLUDED.latest_global_position > beckett.stream_metadata.latest_global_position;
                """);
            streamMetadataCommand.Parameters.Add(new NpgsqlParameter { DataTypeName = DataTypeNames.StreamMetadataArray() });
            batch.BatchCommands.Add(streamMetadataCommand);
        }

        // Step 4: Record message metadata with normalized message types
        if (messageMetadata.Length > 0)
        {
            var messageMetadataCommand = new NpgsqlBatchCommand("""
                WITH message_data AS (
                    SELECT mm.id, mm.global_position, mm.stream_name, mm.stream_position, 
                           mm.message_type_name, mm.category, mm.correlation_id, mm.tenant, mm.timestamp
                    FROM unnest($1) mm
                ),
                message_with_type_ids AS (
                    SELECT md.id, md.global_position, md.stream_name, md.stream_position,
                           mt.id as message_type_id, md.category, md.correlation_id, md.tenant, md.timestamp
                    FROM message_data md
                    INNER JOIN beckett.message_types mt ON md.message_type_name = mt.name
                )
                INSERT INTO beckett.message_metadata 
                    (id, global_position, stream_name, stream_position, message_type_id, category, correlation_id, tenant, timestamp)
                SELECT mwt.id, mwt.global_position, mwt.stream_name, mwt.stream_position, 
                       mwt.message_type_id, mwt.category, mwt.correlation_id, mwt.tenant, mwt.timestamp
                FROM message_with_type_ids mwt
                ON CONFLICT (global_position, id) DO NOTHING;
                """);
            messageMetadataCommand.Parameters.Add(new NpgsqlParameter { DataTypeName = DataTypeNames.MessageMetadataArray() });
            batch.BatchCommands.Add(messageMetadataCommand);
        }

        // Step 5: Update stream types with normalized message types
        if (messageMetadata.Length > 0)
        {
            var streamTypesCommand = new NpgsqlBatchCommand("""
                WITH message_data AS (
                    SELECT mm.stream_name, mm.message_type_name, mm.timestamp
                    FROM unnest($1) mm
                ),
                stream_type_data AS (
                    SELECT md.stream_name, mt.id as message_type_id, 
                           MAX(md.timestamp) as timestamp,
                           COUNT(*) as message_count
                    FROM message_data md
                    INNER JOIN beckett.message_types mt ON md.message_type_name = mt.name
                    GROUP BY md.stream_name, mt.id
                )
                INSERT INTO beckett.stream_types (stream_name, message_type_id, last_seen_at, message_count)
                SELECT std.stream_name, std.message_type_id, std.timestamp, std.message_count
                FROM stream_type_data std
                ON CONFLICT (stream_name, message_type_id) DO UPDATE SET
                    last_seen_at = GREATEST(beckett.stream_types.last_seen_at, EXCLUDED.last_seen_at),
                    message_count = beckett.stream_types.message_count + EXCLUDED.message_count;
                """);
            streamTypesCommand.Parameters.Add(new NpgsqlParameter { DataTypeName = DataTypeNames.MessageMetadataArray() });
            batch.BatchCommands.Add(streamTypesCommand);
        }

        // Step 6: Update categories and tenants (existing logic)
        if (streamMetadata.Length > 0)
        {
            var categoriesTenantsCommand = new NpgsqlBatchCommand("""
                WITH categories_data AS (
                    SELECT sm.category, now() as updated_at
                    FROM unnest($1) sm
                    GROUP BY sm.category
                ),
                tenants_data AS (
                    SELECT mm.tenant
                    FROM unnest($2) mm
                    WHERE mm.tenant IS NOT NULL
                    GROUP BY mm.tenant
                ),
                insert_categories AS (
                    INSERT INTO beckett.categories (name, updated_at)
                    SELECT cd.category, cd.updated_at
                    FROM categories_data cd
                    ON CONFLICT (name) DO UPDATE
                    SET updated_at = EXCLUDED.updated_at
                )
                INSERT INTO beckett.tenants (tenant)
                SELECT td.tenant
                FROM tenants_data td
                ON CONFLICT (tenant) DO NOTHING;
                """);
            categoriesTenantsCommand.Parameters.Add(new NpgsqlParameter { DataTypeName = DataTypeNames.StreamMetadataArray() });
            categoriesTenantsCommand.Parameters.Add(new NpgsqlParameter { DataTypeName = DataTypeNames.MessageMetadataArray() });
            batch.BatchCommands.Add(categoriesTenantsCommand);
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

        if (streamMetadata.Length > 0)
        {
            batch.BatchCommands[commandIndex].Parameters[0].Value = streamMetadata;
            batch.BatchCommands[commandIndex].Parameters[1].Value = messageMetadata;
        }

        await batch.ExecuteNonQueryAsync(cancellationToken);

        return checkpoints.Length;
    }
}