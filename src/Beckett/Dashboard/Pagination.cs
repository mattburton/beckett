namespace Beckett.Dashboard;

public static class Pagination
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 100;

    public static int ToPageParameter(this int? page) => page is null or < 1 ? DefaultPage : page.Value;

    public static int ToPageSizeParameter(this int? pageSize) =>
        pageSize is null or < 1 ? DefaultPageSize : pageSize.Value;

    public static int ToOffset(int page, int pageSize) => (page - 1) * pageSize;
}
