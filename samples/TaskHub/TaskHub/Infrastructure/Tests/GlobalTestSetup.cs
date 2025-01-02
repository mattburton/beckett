using System.Runtime.CompilerServices;

namespace TaskHub.Infrastructure.Tests;

public static class GlobalTestSetup
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        MessageTypeMapInitializer.Initialize(typeof(TaskLists.TaskList).Assembly);
    }
}
