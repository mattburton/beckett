using System.Collections.Concurrent;

namespace Beckett.Database;

public static class Query
{
    private static readonly ConcurrentDictionary<string, string> _registry = [];
    private static PostgresOptions? _options;

    public static void Initialize(PostgresOptions options)
    {
        _options = options;
    }

    public static string Build(string key, string sql, out bool prepare)
    {
        if (_options == null)
        {
            throw new InvalidOperationException("QueryRegistry must be initialized prior to usage");
        }

        prepare = _options.PrepareStatements;

        return _registry.GetOrAdd(
            key,
            _ => _options.Schema == PostgresOptions.DefaultSchema
                ? sql
                : sql.Replace(PostgresOptions.DefaultSchema, _options.Schema)
        );
    }
}
