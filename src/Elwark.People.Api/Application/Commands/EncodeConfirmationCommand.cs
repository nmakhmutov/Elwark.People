using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Api.Infrastructure.Security;
using Elwark.People.Infrastructure.Confirmation;
using MediatR;

namespace Elwark.People.Api.Application.Commands
{
    public class EncodeConfirmationCommand : IRequest<string>
    {
        public EncodeConfirmationCommand(ConfirmationModel confirmation) =>
            Confirmation = confirmation;

        public ConfirmationModel Confirmation { get; }
    }

    public class EncodeConfirmationCommandHandler : IRequestHandler<EncodeConfirmationCommand, string>
    {
        private readonly IDataEncryption _encryption;

        public EncodeConfirmationCommandHandler(IDataEncryption encryption) =>
            _encryption = encryption;

        public Task<string> Handle(EncodeConfirmationCommand request, CancellationToken cancellationToken) =>
            Task.FromResult(_encryption.EncryptToString(request));
    }
}