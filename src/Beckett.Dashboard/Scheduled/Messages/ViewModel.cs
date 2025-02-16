namespace Beckett.Dashboard.Scheduled.Messages;

public record ViewModel(
    IReadOnlyList<ViewModel.Message> Messages,
    string? Query,
    int Page,
    int PageSize,
    int TotalResults
) : IPagedViewModel
{
    public string UrlTemplate => $"{Dashboard.Prefix}/scheduled?page={{0}}&pageSize={{1}}&query={{2}}";

    public record Message(Guid Id, string StreamName, string Type, DateTimeOffset DeliverAt);
}
