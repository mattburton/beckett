using Beckett.Storage.Postgres.Types;
using Npgsql;

namespace Beckett.Storage.Postgres;

public static class NpgsqlDataSourceBuilderExtensions
{
    public static NpgsqlDataSourceBuilder AddBeckett(this NpgsqlDataSourceBuilder builder, string schema = PostgresOptions.DefaultSchema)
    {
        builder.MapComposite<NewStreamEvent>($"{schema}.new_stream_event");

        return builder;
    }
}
