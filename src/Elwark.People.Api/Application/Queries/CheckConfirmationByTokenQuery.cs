using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Api.Application.Commands;
using Elwark.People.Domain.ErrorCodes;
using Elwark.People.Domain.Exceptions;
using Elwark.People.Infrastructure.Confirmation;
using MediatR;

namespace Elwark.People.Api.Application.Queries
{
    public class CheckConfirmationByTokenQuery : IRequest<ConfirmationModel>
    {
        public CheckConfirmationByTokenQuery(string token) =>
            Token = token;

        public string Token { get; }
    }

    public class CheckConfirmationByTokenQueryHandler : IRequestHandler<CheckConfirmationByTokenQuery, ConfirmationModel>
    {
        private readonly IConfirmationStore _store;
        private readonly IMediator _mediator;

        public CheckConfirmationByTokenQueryHandler(IConfirmationStore store, IMediator mediator)
        {
            _store = store;
            _mediator = mediator;
        }

        public async Task<ConfirmationModel> Handle(CheckConfirmationByTokenQuery request, CancellationToken ct)
        {
            var data = await _mediator.Send(new DecodeConfirmationCommand(request.Token), ct);
            var confirmation = await _store.GetAsync(data.IdentityId, data.Type)
                               ?? throw new ElwarkConfirmationException(ConfirmationError.NotFound);

            if (confirmation.Code != data.Code)
                throw new ElwarkConfirmationException(ConfirmationError.NotMatch);

            return data;
        }
    }
}