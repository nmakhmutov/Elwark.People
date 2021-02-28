using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using MongoDB.Bson;
using People.Api.Application.Commands;
using People.Api.Application.Queries;
using People.Api.Mappers;
using People.Domain;
using People.Domain.AggregateModels.Account.Identities;
using People.Domain.Exceptions;
using People.Grpc.Common;
using People.Grpc.Gateway;
using People.Infrastructure.Countries;
using People.Infrastructure.Timezones;
using AccountId = People.Grpc.Common.AccountId;
using Country = People.Grpc.Gateway.Country;
using Timezone = People.Grpc.Gateway.Timezone;

namespace People.Api.Grpc
{
    public class GatewayService : Gateway.GatewayBase
    {
        private readonly IMediator _mediator;

        public GatewayService(IMediator mediator) =>
            _mediator = mediator;

        public override async Task<AccountReply> GetAccount(AccountId request, ServerCallContext context)
        {
            var data = await _mediator.Send(new GetAccountByIdQuery(request.Value), context.CancellationToken);
            if (data is not null)
                return data.ToGatewayAccountReply();

            context.Status = new Status(StatusCode.NotFound, ElwarkExceptionCodes.AccountNotFound);
            return new AccountReply();
        }
    }
}