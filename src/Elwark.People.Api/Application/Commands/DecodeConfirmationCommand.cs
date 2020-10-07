using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Api.Infrastructure.Security;
using Elwark.People.Domain.ErrorCodes;
using Elwark.People.Domain.Exceptions;
using Elwark.People.Infrastructure.Confirmation;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Elwark.People.Api.Application.Commands
{
    public class DecodeConfirmationCommand : IRequest<ConfirmationModel>
    {
        public DecodeConfirmationCommand(string token) =>
            Token = token;

        public string Token { get; }
    }

    public class DecodeConfirmationCommandHandler : IRequestHandler<DecodeConfirmationCommand, ConfirmationModel>
    {
        private readonly IDataEncryption _encryption;
        private readonly ILogger<DecodeConfirmationCommandHandler> _logger;

        public DecodeConfirmationCommandHandler(IDataEncryption encryption,
            ILogger<DecodeConfirmationCommandHandler> logger)
        {
            _encryption = encryption;
            _logger = logger;
        }

        public Task<ConfirmationModel> Handle(DecodeConfirmationCommand request, CancellationToken cancellationToken)
        {
            try
            {
                return Task.FromResult(_encryption.DecryptFromString<ConfirmationModel>(request.Token));
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Confirmation manager format exception");
                return Task.FromException<ConfirmationModel>(
                    new ElwarkCryptographyException(CryptographyError.InvalidFormat, ex));
            }
            catch (DecoderFallbackException ex)
            {
                _logger.LogError(ex, "Confirmation manager decode fallback exception");
                return Task.FromException<ConfirmationModel>(
                    new ElwarkCryptographyException(CryptographyError.DecoderError, ex));
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Confirmation manager json exception");
                return Task.FromException<ConfirmationModel>(
                    new ElwarkCryptographyException(CryptographyError.JsonError, ex));
            }
            catch (CryptographicException ex)
            {
                _logger.LogError(ex, "Confirmation manager cryptographic exception");
                return Task.FromException<ConfirmationModel>(
                    new ElwarkCryptographyException(CryptographyError.DecoderError, ex));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Confirmation manager unknown exception");
                return Task.FromException<ConfirmationModel>(
                    new ElwarkCryptographyException(CryptographyError.UnknownError, ex));
            }
        }
    }
}