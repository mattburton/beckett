using System.Runtime.CompilerServices;

namespace Beckett.Tests;

public static class GlobalTestSetup
{
    [ModuleInitializer]
    internal static void Initialize() => MessageRegistry.Register();
}
