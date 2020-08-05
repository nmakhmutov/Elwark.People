using System;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Api.Infrastructure.Services.Confirmation;
using Elwark.People.Domain.ErrorCodes;
using Elwark.People.Domain.Exceptions;
using Elwark.People.Infrastructure.Confirmation;
using MediatR;

namespace Elwark.People.Api.Application.Queries
{
    public class CheckConfirmationByTokenQuery : IRequest<ConfirmationData>
    {
        public CheckConfirmationByTokenQuery(string token) =>
            Token = token;

        public string Token { get; }
    }

    public class CheckConfirmationByTokenQueryHandler : IRequestHandler<CheckConfirmationByTokenQuery, ConfirmationData>
    {
        private readonly IConfirmationService _service;
        private readonly IConfirmationStore _store;

        public CheckConfirmationByTokenQueryHandler(IConfirmationService service, IConfirmationStore store)
        {
            _service = service;
            _store = store;
        }

        public async Task<ConfirmationData> Handle(CheckConfirmationByTokenQuery request,
            CancellationToken cancellationToken)
        {
            var data = _service.ReadToken(request.Token);
            var confirmation = await _store.GetAsync(data.ConfirmationId, cancellationToken)
                               ?? throw new ElwarkConfirmationException(ConfirmationError.NotFound);

            if (confirmation.Code != data.Code ||
                confirmation.IdentityId != data.IdentityId ||
                confirmation.Type != data.Type)
                throw new ElwarkConfirmationException(ConfirmationError.NotMatch);

            if (confirmation.ExpiredAt < DateTimeOffset.UtcNow)
                throw new ElwarkConfirmationException(ConfirmationError.Expired);

            return data;
        }
    }
}