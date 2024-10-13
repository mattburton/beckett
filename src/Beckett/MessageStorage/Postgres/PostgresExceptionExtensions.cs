using Npgsql;

namespace Beckett.MessageStorage.Postgres;

public static class PostgresExceptionExtensions
{
    public static void HandleAppendToStreamError(this PostgresException exception)
    {
        const string streamDoesNotExistText = "non-existing stream";
        const string streamAlreadyExistsText = "stream that already exists";
        const string expectedVersionText = "expected version";

        if (exception.MessageText.Contains(streamDoesNotExistText))
        {
            throw new StreamDoesNotExistException(exception.MessageText);
        }

        if (exception.MessageText.Contains(streamAlreadyExistsText))
        {
            throw new StreamAlreadyExistsException(exception.MessageText);
        }

        if (exception.MessageText.Contains(expectedVersionText))
        {
            throw new OptimisticConcurrencyException(exception.MessageText);
        }
    }
}

