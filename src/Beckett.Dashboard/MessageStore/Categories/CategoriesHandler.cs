using Beckett.Dashboard.MessageStore.Components;

namespace Beckett.Dashboard.MessageStore.Categories;

public static class CategoriesHandler
{
    public static async Task<IResult> Get(
        HttpContext context,
        string? query,
        int? page,
        int? pageSize,
        IDashboard dashboard,
        CancellationToken cancellationToken
    )
    {
        var tenant = TenantFilter.GetCurrentTenant(context);
        var pageParameter = page.ToPageParameter();
        var pageSizeParameter = pageSize.ToPageSizeParameter();

        var result = await dashboard.MessageStore.GetCategories(
            tenant,
            query,
            pageParameter,
            pageSizeParameter,
            cancellationToken
        );

        return Results.Extensions.Render<Categories>(
            new Categories.ViewModel(
                result.Categories,
                query,
                pageParameter,
                pageSizeParameter,
                result.TotalResults
            )
        );
    }
}
