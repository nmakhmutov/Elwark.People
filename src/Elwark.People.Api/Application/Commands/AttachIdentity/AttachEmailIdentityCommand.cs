using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using Elwark.People.Domain.Exceptions;
using MediatR;

namespace Elwark.People.Api.Application.Commands.AttachIdentity
{
    public class AttachEmailIdentityCommand : IRequest
    {
        public AttachEmailIdentityCommand(AccountId id, Identification.Email email)
        {
            Id = id;
            Email = email;
        }

        public AccountId Id { get; }

        public Identification.Email Email { get; }
    }

    public class AttachEmailIdentityCommandHandler : IRequestHandler<AttachEmailIdentityCommand>
    {
        private readonly IAccountRepository _repository;
        private readonly IIdentificationValidator _validator;

        public AttachEmailIdentityCommandHandler(IAccountRepository repository, IIdentificationValidator validator)
        {
            _repository = repository;
            _validator = validator;
        }

        public async Task<Unit> Handle(AttachEmailIdentityCommand request, CancellationToken cancellationToken)
        {
            var account = await _repository.GetAsync(request.Id, cancellationToken)
                          ?? throw ElwarkAccountException.NotFound(request.Id);

            await account.AddIdentificationAsync(request.Email, _validator);

            _repository.Update(account);
            await _repository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}