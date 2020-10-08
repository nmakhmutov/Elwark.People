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
    public class CheckConfirmationQuery : IRequest<ConfirmationModel>
    {
        public CheckConfirmationQuery(IdentityId identityId, ConfirmationType type, long code)
        {
            IdentityId = identityId;
            Type = type;
            Code = code;
        }

        public IdentityId IdentityId { get; }

        public ConfirmationType Type { get; }

        public long Code { get; }
    }
    
    public class CheckConfirmationQueryHandler : IRequestHandler<CheckConfirmationQuery, ConfirmationModel>
    {
        private readonly IConfirmationStore _store;

        public CheckConfirmationQueryHandler(IConfirmationStore store)
        {
            _store = store;
        }

        public async Task<ConfirmationModel> Handle(CheckConfirmationQuery request, CancellationToken cancellationToken)
        {
            var confirmation = await _store.GetAsync(request.IdentityId, request.Type)
                               ?? throw new ElwarkConfirmationException(ConfirmationError.NotFound);

            if (confirmation.Code != request.Code)
                throw new ElwarkConfirmationException(ConfirmationError.NotMatch);

            return confirmation;
        }
    }
}