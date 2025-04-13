using Core.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Projections;

public class Configuration : IConfigureServices
{
    public void Services(IServiceCollection services)
    {
        services.Scan(
            x => x.FromApplicationDependencies()
                .AddClasses(classes => classes.AssignableTo(typeof(IProjection<>)))
                .AsImplementedInterfaces()
                .WithTransientLifetime()
        );

        services.AddSingleton(typeof(IProjector<>), typeof(Projector<>));
    }
}
