using System.Runtime.CompilerServices;

namespace Tests;

public static class GlobalTestSetup
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        MessageTypeMapInitializer.Initialize(typeof(TaskHub.TaskList.TaskList).Assembly);
    }
}
