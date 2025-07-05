using Beckett.Database;

namespace Beckett.Dashboard.MessageStore.Categories;

public static class CategoriesEndpoint
{
    public static async Task<IResult> Handle(
        HttpContext context,
        string? query,
        int? page,
        int? pageSize,
        IPostgresDatabase database,
        PostgresOptions options,
        CancellationToken cancellationToken
    )
    {
        var pageParameter = page.ToPageParameter();
        var pageSizeParameter = pageSize.ToPageSizeParameter();
        var offset = Pagination.ToOffset(pageParameter, pageSizeParameter);

        var result = await database.Execute(
            new CategoriesQuery(query, offset, pageSizeParameter),
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
