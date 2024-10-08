using Npgsql;

namespace Beckett.Scheduling;

public interface ITransactionalMessageScheduler
{
    Task<Guid> ScheduleMessage(
        string streamName,
        Message message,
        DateTimeOffset deliverAt,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken
    );
}
