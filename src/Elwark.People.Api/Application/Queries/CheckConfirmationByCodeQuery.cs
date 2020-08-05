using System;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Api.Infrastructure.Services.Confirmation;
using Elwark.People.Domain.ErrorCodes;
using Elwark.People.Domain.Exceptions;
using Elwark.People.Infrastructure.Confirmation;
using Elwark.People.Shared.Primitives;
using MediatR;

namespace Elwark.People.Api.Application.Queries
{
    public class CheckConfirmationByCodeQuery : IRequest<ConfirmationData>
    {
        public CheckConfirmationByCodeQuery(IdentityId identityId, long code, ConfirmationType type)
        {
            IdentityId = identityId;
            Code = code;
            Type = type;
        }

        public long Code { get; }
        public IdentityId IdentityId { get; }

        public ConfirmationType Type { get; }
    }

    public class CheckConfirmationByCodeQueryHandler : IRequestHandler<CheckConfirmationByCodeQuery, ConfirmationData>
    {
        private readonly IConfirmationStore _store;

        public CheckConfirmationByCodeQueryHandler(IConfirmationStore store) =>
            _store = store;

        public async Task<ConfirmationData> Handle(CheckConfirmationByCodeQuery request,
            CancellationToken cancellationToken)
        {
            var confirmation = await _store.GetAsync(request.IdentityId, request.Code, cancellationToken)
                               ?? throw new ElwarkConfirmationException(ConfirmationError.NotFound);

            if (confirmation.Type != request.Type)
                throw new ElwarkConfirmationException(ConfirmationError.NotMatch);

            if (confirmation.ExpiredAt < DateTimeOffset.UtcNow)
                throw new ElwarkConfirmationException(ConfirmationError.Expired);

            return new ConfirmationData(confirmation.Id, confirmation.IdentityId, confirmation.Type, confirmation.Code);
        }
    }
}