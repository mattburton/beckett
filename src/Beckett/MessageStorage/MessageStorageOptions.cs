using Beckett.MessageStorage.Postgres;

namespace Beckett.MessageStorage;

public class MessageStorageOptions
{
    internal Type MessageStorageType { get; set; } = typeof(PostgresMessageStorage);
}
