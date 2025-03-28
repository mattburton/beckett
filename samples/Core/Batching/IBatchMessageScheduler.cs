using Beckett;
using Npgsql;

namespace Core.Batching;

public interface IBatchMessageScheduler
{
    NpgsqlBatchCommand ScheduleMessage(
        string streamName,
        Message message,
        DateTimeOffset deliverAt
    );
}
