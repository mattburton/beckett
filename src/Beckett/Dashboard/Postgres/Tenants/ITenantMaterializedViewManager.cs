namespace Beckett.Dashboard.Postgres.Tenants;

public interface ITenantMaterializedViewManager
{
    Task Refresh(CancellationToken cancellationToken);
}
