namespace Beckett.Dashboard;

public interface IDashboardMessageStore
{
    Task<GetTenantsResult> GetTenants(CancellationToken cancellationToken);

    Task<GetCategoriesResult> GetCategories(
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );

    Task<GetCategoryStreamsResult> GetCategoryStreams(
        string tenant,
        string category,
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );

    Task<GetCorrelatedMessagesResult> GetCorrelatedMessages(
        string correlationId,
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );

    Task<GetMessageResult?> GetMessage(string id, CancellationToken cancellationToken);

    Task<GetMessageResult?> GetMessage(string streamName, long streamPosition, CancellationToken cancellationToken);

    Task<GetStreamMessagesResult> GetStreamMessages(
        string streamName,
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );
}

public record GetTenantsResult(List<string> Tenants);

public record GetCategoriesResult(List<GetCategoriesResult.Category> Categories, int TotalResults)
{
    public record Category(string Name, DateTimeOffset LastUpdated);
}

public record GetCategoryStreamsResult(List<GetCategoryStreamsResult.Stream> Streams, int TotalResults)
{
    public record Stream(string StreamName, DateTimeOffset LastUpdated);
}

public record GetCorrelatedMessagesResult(List<GetCorrelatedMessagesResult.Message> Messages, int TotalResults)
{
    public record Message(Guid Id, string StreamName, int StreamPosition, string Type, DateTimeOffset Timestamp);
}

public record GetMessageResult(
    string Id,
    string Category,
    string StreamName,
    long GlobalPosition,
    long StreamPosition,
    long StreamVersion,
    string Type,
    DateTimeOffset Timestamp,
    string Data,
    Dictionary<string, string> Metadata
);

public record GetStreamMessagesResult(List<GetStreamMessagesResult.Message> Messages, int TotalResults)
{
    public record Message(Guid Id, int StreamPosition, string Type, DateTimeOffset Timestamp);
}
