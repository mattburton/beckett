using Contracts.Users.Queries;
using Core.Streams;
using TaskHub.Users.Streams;

namespace TaskHub.Users.Queries.GetUser;

public class GetUserQueryHandler(IStreamReader reader) : IQueryHandler<GetUserQuery, GetUserQuery.Result?>
{
    public async Task<GetUserQuery.Result?> Handle(GetUserQuery query, CancellationToken cancellationToken)
    {
        var stream = await reader.ReadStream(new UserStream(query.Username), cancellationToken);

        if (stream.IsEmpty)
        {
            return null;
        }

        var model = stream.ProjectTo<UserReadModel>();

        return new GetUserQuery.Result(model.Username, model.Email);
    }
}
