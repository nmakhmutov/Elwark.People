using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Api.Infrastructure.Security;
using Elwark.People.Infrastructure.Confirmation;
using MediatR;

namespace Elwark.People.Api.Application.Queries
{
    public class EncodeConfirmationQuery : IRequest<string>
    {
        public EncodeConfirmationQuery(ConfirmationModel confirmation) =>
            Confirmation = confirmation;

        public ConfirmationModel Confirmation { get; }
    }

    public class EncodeConfirmationQueryHandler : IRequestHandler<EncodeConfirmationQuery, string>
    {
        private readonly IDataEncryption _encryption;

        public EncodeConfirmationQueryHandler(IDataEncryption encryption) =>
            _encryption = encryption;

        public Task<string> Handle(EncodeConfirmationQuery request, CancellationToken cancellationToken) =>
            Task.FromResult(_encryption.EncryptToString(request));
    }
}