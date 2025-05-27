using Beckett.Storage.Postgres;

namespace Beckett.Storage;

public class MessageStorageOptions
{
    internal Type MessageStorageType { get; set; } = typeof(PostgresMessageStorage);
}
