using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Infrastructure.Confirmation;
using Elwark.People.Shared.Primitives;
using MediatR;

namespace Elwark.People.Api.Application.Commands
{
    public class DeleteConfirmationCommand : IRequest
    {
        public DeleteConfirmationCommand(IdentityId id, ConfirmationType type)
        {
            Id = id;
            Type = type;
        }

        public IdentityId Id { get; }
        
        public ConfirmationType Type { get; }
    }

    public class DeleteConfirmationCommandHandler : IRequestHandler<DeleteConfirmationCommand>
    {
        private readonly IConfirmationStore _store;

        public DeleteConfirmationCommandHandler(IConfirmationStore store) =>
            _store = store;

        public async Task<Unit> Handle(DeleteConfirmationCommand request, CancellationToken cancellationToken)
        {
            await _store.DeleteAsync(request.Id, request.Type);

            return Unit.Value;
        }
    }
}