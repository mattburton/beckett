using Beckett.Dashboard.Administration.RefreshTenants;

namespace Beckett.Dashboard.Administration;

public class Routes : IConfigureRoutes
{
    public void Configure(IEndpointRouteBuilder builder)
    {
        var routes = builder.MapGroup("/administration");

        routes.MapPost("/refresh-tenants", RefreshTenantsEndpoint.Handle);
    }
}
