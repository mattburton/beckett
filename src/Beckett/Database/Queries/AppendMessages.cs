using Beckett.Database.Types;
using Npgsql;

namespace Beckett.Database.Queries;

public class AppendMessages(
    MessageType[] messages
) : IPostgresDatabaseQuery<int>
{
    public async Task<int> Execute(NpgsqlCommand command, string schema, CancellationToken cancellationToken)
    {
        try
        {
            command.CommandText = $"select {schema}.append_messages($1);";

            command.Parameters.Add(new NpgsqlParameter { DataTypeName = DataTypeNames.MessageArray(schema) });

            await command.PrepareAsync(cancellationToken);

            command.Parameters[0].Value = messages;

            return await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresException e)
        {
            const string streamDoesNotExistText = "non-existing stream";
            const string streamAlreadyExistsText = "stream that already exists";
            const string expectedVersionText = "expected version";

            if (e.MessageText.Contains(streamDoesNotExistText))
            {
                throw new StreamDoesNotExistException(e.MessageText);
            }

            if (e.MessageText.Contains(streamAlreadyExistsText))
            {
                throw new StreamAlreadyExistsException(e.MessageText);
            }

            if (e.MessageText.Contains(expectedVersionText))
            {
                throw new OptimisticConcurrencyException(e.MessageText);
            }

            throw;
        }
    }
}
