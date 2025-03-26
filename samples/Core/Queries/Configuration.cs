using Core.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Queries;

public class Configuration : IConfigureServices
{
    public void Services(IServiceCollection services)
    {
        services.AddSingleton<IQueryBus, QueryBus>();

        services.Scan(
            x => x.FromApplicationDependencies().AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)))
                .AsImplementedInterfaces().WithTransientLifetime()
        );
    }
}
