namespace Beckett.Dashboard;

public static class Pagination
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;

    public static int ToPage(this int? page) => page is null or < 1 ? DefaultPage : page.Value;

    public static int ToPageSize(this int? pageSize) => pageSize is null or < 1 ? DefaultPage : pageSize.Value;

    public static int ToOffset(int page, int pageSize) => (page - 1) * pageSize;
}
