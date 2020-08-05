using System;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Infrastructure.Confirmation;
using MediatR;

namespace Elwark.People.Api.Application.Commands
{
    public class DeleteConfirmationCommand : IRequest
    {
        public DeleteConfirmationCommand(Guid confirmationId) =>
            ConfirmationId = confirmationId;

        public Guid ConfirmationId { get; }
    }

    public class DeleteConfirmationCommandHandler : IRequestHandler<DeleteConfirmationCommand>
    {
        private readonly IConfirmationStore _store;

        public DeleteConfirmationCommandHandler(IConfirmationStore store) =>
            _store = store;

        public async Task<Unit> Handle(DeleteConfirmationCommand request, CancellationToken cancellationToken)
        {
            await _store.DeleteAsync(request.ConfirmationId, cancellationToken);

            return Unit.Value;
        }
    }
}