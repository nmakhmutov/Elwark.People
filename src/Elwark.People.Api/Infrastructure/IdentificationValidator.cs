using System;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.Queries;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using Elwark.People.Domain.ErrorCodes;
using Elwark.People.Domain.Exceptions;
using Elwark.Storage.Client;
using MediatR;

namespace Elwark.People.Api.Infrastructure
{
    public class IdentificationValidator : IIdentificationValidator
    {
        private readonly IElwarkStorageClient _client;
        private readonly IMediator _mediator;

        public IdentificationValidator(IMediator mediator, IElwarkStorageClient client)
        {
            _mediator = mediator;
            _client = client;
        }

        public async Task CheckUniquenessAsync(Identification identification, CancellationToken ct)
        {
            switch (identification)
            {
                case null:
                    throw new ArgumentNullException(nameof(identification));
                
                case Identification.Email email when await _client.Blacklist.IsForbiddenEmailDomainAsync(email.Value, ct):
                    throw new ElwarkIdentificationException(IdentificationError.Forbidden, identification);
            }

            var identity = await _mediator.Send(new GetIdentityByIdentifierQuery(identification), ct);

            if (identity is null)
                return;

            if (identity.ConfirmedAt.HasValue)
                throw new ElwarkIdentificationException(IdentificationError.AlreadyRegistered, identification);

            throw new ElwarkIdentificationException(IdentificationError.NotConfirmed, identification);
        }
    }
}