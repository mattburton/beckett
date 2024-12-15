namespace Beckett.Dashboard;

public interface IPagedViewModel
{
    string? Query { get; }
    int Page { get; }
    int PageSize { get; }
    int TotalResults { get; }
    string UrlTemplate { get; }

    int From => TotalResults == 0 ? 0 : Page == 1 ? 1 : (Page - 1) * PageSize + 1;
    int To => From + PageSize > TotalResults ? TotalResults : From - 1 + PageSize;
    int TotalPages => (int)Math.Ceiling((double)TotalResults / PageSize);
}

public static class PagedViewModelExtensions
{
    public static string CurrentLink(this IPagedViewModel model) => string.Format(model.UrlTemplate, model.Page, model.PageSize, model.Query);

    public static string PreviousLink(this IPagedViewModel model) => model.Page > 1 ? string.Format(model.UrlTemplate, model.Page - 1, model.PageSize, model.Query) : "#";

    public static string NextLink(this IPagedViewModel model) => model.Page < model.TotalPages ? string.Format(model.UrlTemplate, model.Page + 1, model.PageSize, model.Query) : "#";
}
