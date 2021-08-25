using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MongoDB.Bson;
using People.Api.Application.Commands.SendConfirmation;
using People.Domain;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands.ResendSignUpConfirmation
{
    public sealed record ResendSignUpConfirmationCommand(AccountId Id, Language Language) : IRequest<ObjectId>;

    public sealed class ResendSignUpConfirmationCommandHandler 
        : IRequestHandler<ResendSignUpConfirmationCommand, ObjectId>
    {
        private readonly IMediator _mediator;
        private readonly IAccountRepository _repository;

        public ResendSignUpConfirmationCommandHandler(IAccountRepository repository, IMediator mediator)
        {
            _repository = repository;
            _mediator = mediator;
        }

        public async Task<ObjectId> Handle(ResendSignUpConfirmationCommand request, CancellationToken ct)
        {
            var account = await _repository.GetAsync(request.Id, ct);
            if (account is null)
                throw new ElwarkException(ElwarkExceptionCodes.AccountNotFound);

            if (account.IsConfirmed())
                throw new ElwarkException(ElwarkExceptionCodes.IdentityAlreadyConfirmed);

            var command = new SendConfirmationCommand(account.Id, account.GetPrimaryEmail().Identity, request.Language); 
            
            return await _mediator.Send(command, ct);
        }
    }
}
