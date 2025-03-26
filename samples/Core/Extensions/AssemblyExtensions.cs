using System.Reflection;

namespace Core.Extensions;

public static class AssemblyExtensions
{
    public static IReadOnlyCollection<Type> GetLoadableTypes(this Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null).ToArray()!;
        }
        catch
        {
            return [];
        }
    }
}
