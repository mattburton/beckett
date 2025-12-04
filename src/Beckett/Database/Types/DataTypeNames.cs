namespace Beckett.Database.Types;

public static class DataTypeNames
{
    private static string Schema = PostgresOptions.DefaultSchema;

    public static void Initialize(PostgresOptions options) => Schema = options.Schema;

    public static string CheckpointStatus(string? schema = null) => $"{schema ?? Schema}.checkpoint_status";

    public static string Checkpoint(string? schema = null) => $"{schema ?? Schema}.checkpoint";

    public static string CheckpointArray(string? schema = null) => $"{schema ?? Schema}.checkpoint[]";

    public static string Message(string? schema = null) => $"{schema ?? Schema}.message";

    public static string MessageArray(string? schema = null) => $"{schema ?? Schema}.message[]";

    public static string Retry(string? schema = null) => $"{schema ?? Schema}.retry";

    public static string SubscriptionStatus(string? schema = null) => $"{schema ?? Schema}.subscription_status";
}
