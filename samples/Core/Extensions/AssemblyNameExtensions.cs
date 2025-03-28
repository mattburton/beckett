using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Core.Extensions;

public static class AssemblyNameExtensions
{
    public static bool TryLoadAssembly(
        this AssemblyName assemblyName,
        [NotNullWhen(returnValue: true)] out Assembly? assembly
    )
    {
        try
        {
            assembly = Assembly.Load(assemblyName);

            return true;
        }
        catch
        {
            assembly = null;

            return false;
        }
    }
}
