using System.Reflection;
using Beckett.Database;
using Beckett.Subscriptions;

namespace Beckett;

public class BeckettOptions
{
    internal Assembly[] Assemblies { get; set; } = [];

    public DatabaseOptions Database { get; } = new();
    public SubscriptionOptions Subscriptions { get; } = new();

    public void UseAssemblies(params Assembly[] assemblies)
    {
        Assemblies = assemblies;
    }

    internal IEnumerable<Type> GetAssemblyTypes()
    {
        return Assemblies.SelectMany(x =>
        {
            try
            {
                return x.GetTypes();
            }
            catch (ReflectionTypeLoadException)
            {
                return Array.Empty<Type>();
            }
        });
    }
}
