namespace Beckett.Database.Types;

public static class DataTypeNames
{
    public static string CheckpointStatus(string schema) => $"{schema}.checkpoint_status";

    public static string Checkpoint(string schema) => $"{schema}.checkpoint";

    public static string CheckpointArray(string schema) => $"{schema}.checkpoint[]";

    public static string Message(string schema) => $"{schema}.message";

    public static string MessageArray(string schema) => $"{schema}.message[]";

    public static string ScheduledMessage(string schema) => $"{schema}.scheduled_message";
}
