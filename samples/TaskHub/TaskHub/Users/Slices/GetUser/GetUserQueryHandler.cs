using TaskHub.Users.Contracts.Queries;

namespace TaskHub.Users.Slices.GetUser;

public class GetUserQueryHandler(
    IMessageStore messageStore
) : ProjectedStreamQueryHandler<GetUserQuery, GetUserReadModel>(messageStore)
{
    protected override string StreamName(GetUserQuery query) => UserModule.StreamName(query.Username);

    public override async Task<GetUserReadModel?> Handle(GetUserQuery query, CancellationToken cancellationToken)
    {
        var result = await base.Handle(query, cancellationToken);

        return result is { Deleted: false } ? result : null;
    }
}
