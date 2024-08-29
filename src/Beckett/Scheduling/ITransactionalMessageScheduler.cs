using Npgsql;

namespace Beckett.Scheduling;

public interface ITransactionalMessageScheduler
{
    Task<Guid> ScheduleMessage(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string streamName,
        object message,
        DateTimeOffset deliverAt,
        CancellationToken cancellationToken
    );
}
