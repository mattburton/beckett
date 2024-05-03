using Microsoft.Extensions.DependencyInjection;

namespace Beckett;

public interface IConfigureBeckett
{
    void Configure(IServiceCollection services, BeckettOptions options);
}
