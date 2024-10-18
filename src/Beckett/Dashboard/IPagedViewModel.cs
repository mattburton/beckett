namespace Beckett.Dashboard;

public interface IPagedViewModel
{
    string? Query { get; }
    int Page { get; }
    int PageSize { get; }
    int TotalResults { get; }

    int From => TotalResults == 0 ? 0 : Page == 1 ? 1 : (Page - 1) * PageSize + 1;
    int To => From + PageSize > TotalResults ? TotalResults : From - 1 + PageSize;
    int TotalPages => (int)Math.Ceiling((double)TotalResults / PageSize);

    string PreviousLink(string urlTemplate) => Page > 1 ? string.Format(urlTemplate, Page - 1, PageSize, Query) : "#";

    string NextLink(string urlTemplate) =>
        Page < TotalPages ? string.Format(urlTemplate, Page + 1, PageSize, Query) : "#";
}
