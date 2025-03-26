namespace Core.Modules;

public static class TypeExtensions
{
    private static readonly Type ModuleType = typeof(IModule);

    public static bool IsModule(this Type x) =>
        ModuleType.IsAssignableFrom(x) && x is { IsAbstract: false, IsInterface: false };
}
