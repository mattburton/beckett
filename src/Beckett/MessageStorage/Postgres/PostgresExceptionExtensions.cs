using Npgsql;

namespace Beckett.MessageStorage.Postgres;

public static class PostgresExceptionExtensions
{
    public static void HandleAppendToStreamError(
        this PostgresException e,
        string streamName,
        long expectedVersion
    )
    {
        const string streamDoesNotExistText = "non-existing stream";
        const string streamAlreadyExistsText = "stream that already exists";
        const string expectedVersionText = "expected version";
        const string uniqueConstraintViolation = "23505";

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

        if (e.SqlState == uniqueConstraintViolation)
        {
            throw new OptimisticConcurrencyException(
                $"Stream {streamName} version does not match expected version {expectedVersion}"
            );
        }
    }
}

