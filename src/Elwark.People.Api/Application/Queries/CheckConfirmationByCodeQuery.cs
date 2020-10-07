using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Domain.ErrorCodes;
using Elwark.People.Domain.Exceptions;
using Elwark.People.Infrastructure.Confirmation;
using Elwark.People.Shared.Primitives;
using MediatR;

namespace Elwark.People.Api.Application.Queries
{
    public class CheckConfirmationByCodeQuery : IRequest<ConfirmationModel>
    {
        public CheckConfirmationByCodeQuery(IdentityId identityId, ConfirmationType type, long code)
        {
            IdentityId = identityId;
            Code = code;
            Type = type;
        }
        
        public IdentityId IdentityId { get; }

        public ConfirmationType Type { get; }
        
        public long Code { get; }
    }

    public class CheckConfirmationByCodeQueryHandler : IRequestHandler<CheckConfirmationByCodeQuery, ConfirmationModel>
    {
        private readonly IConfirmationStore _store;

        public CheckConfirmationByCodeQueryHandler(IConfirmationStore store) =>
            _store = store;

        public async Task<ConfirmationModel> Handle(CheckConfirmationByCodeQuery request, CancellationToken ct)
        {
            var confirmation = await _store.GetAsync(request.IdentityId, request.Type)
                               ?? throw new ElwarkConfirmationException(ConfirmationError.NotFound);

            if (confirmation.Code != request.Code)
                throw new ElwarkConfirmationException(ConfirmationError.NotMatch);

            return confirmation;
        }
    }
}