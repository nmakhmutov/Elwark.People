using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Domain.AggregateModels.Account;
using People.Infrastructure.Confirmations;

namespace People.Api.Application.Commands
{
    public sealed record SendPrimaryEmailConfirmationCommand(AccountId Id, AccountEmail Email)
        : IRequest;

    internal sealed class SendPrimaryEmailConfirmationCommandHandler : IRequestHandler<SendPrimaryEmailConfirmationCommand>
    {
        private readonly IConfirmationService _confirmation;

        public SendPrimaryEmailConfirmationCommandHandler(IConfirmationService confirmation) =>
            _confirmation = confirmation;

        public async Task<Unit> Handle(SendPrimaryEmailConfirmationCommand request, CancellationToken ct)
        {
            var code = await _confirmation.CreateSignUpConfirmation(request.Id, ct);
            
            return Unit.Value;
        }
    }
}