using Beckett;
using Npgsql;

namespace Core.Batching;

public interface IBatchMessageStorage
{
    NpgsqlBatchCommand AppendToStream(
        string streamName,
        ExpectedVersion expectedVersion,
        IReadOnlyList<Message> messages
    );
}
