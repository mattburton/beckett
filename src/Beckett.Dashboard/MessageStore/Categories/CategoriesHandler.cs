namespace Beckett.Dashboard.MessageStore.Categories;

public static class CategoriesHandler
{
    public static async Task<IResult> Get(
        string? query,
        int? page,
        int? pageSize,
        IDashboard dashboard,
        CancellationToken cancellationToken
    )
    {
        var pageParameter = page.ToPageParameter();
        var pageSizeParameter = pageSize.ToPageSizeParameter();

        var result = await dashboard.MessageStore.GetCategories(
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