namespace Beckett.Messages;

public class MessageOptions
{
    public bool AllowDynamicTypeMapping { get; set; } = true;

    public UnknownMessageTypePolicy UnknownMessageTypePolicy { get; set; } =
        UnknownMessageTypePolicy.LogErrorAndContinue;
}

public enum UnknownMessageTypePolicy
{
    IgnoreAndContinue,
    LogErrorAndContinue,
    LogErrorAndExitApplication
}
