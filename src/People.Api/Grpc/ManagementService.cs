using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using People.Api.Application.Queries.GetAccounts;
using People.Api.Mappers;
using People.Grpc.Gateway;

namespace People.Api.Grpc;

internal sealed class ManagementService : PeopleManagement.PeopleManagementBase
{
    private readonly IMediator _mediator;

    public ManagementService(IMediator mediator) =>
        _mediator = mediator;

    public override async Task<ManagementPageAccountsReply> GetAccounts(ManagementAccountsRequest request,
        ServerCallContext context)
    {
        var query = new GetAccountsQuery(request.Page, request.Limit);
        var (items, pages, count) = await _mediator.Send(query, context.CancellationToken);

        return new ManagementPageAccountsReply
        {
            Count = count,
            Pages = pages,
            Topics =
            {
                items.Select(x => new ManagementPageAccountsReply.Types.Account
                {
                    Id = x.Id.ToGrpc(),
                    Language = x.Language.ToString(),
                    Name = x.Name.ToGrpc(),
                    Picture = x.Picture.ToString(),
                    CountryCode = x.CountryCode.ToString(),
                    CreatedAt = x.CreatedAt.ToTimestamp(),
                    TimeZone = x.TimeZone
                })
            }
        };
    }
}
