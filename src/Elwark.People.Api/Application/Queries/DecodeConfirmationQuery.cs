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

namespace Elwark.People.Api.Application.Queries
{
    public class DecodeConfirmationQuery : IRequest<ConfirmationModel>
    {
        public DecodeConfirmationQuery(string token) =>
            Token = token;

        public string Token { get; }
    }

    public class DecodeConfirmationQueryHandler : IRequestHandler<DecodeConfirmationQuery, ConfirmationModel>
    {
        private readonly IDataEncryption _encryption;
        private readonly ILogger<DecodeConfirmationQueryHandler> _logger;

        public DecodeConfirmationQueryHandler(IDataEncryption encryption,
            ILogger<DecodeConfirmationQueryHandler> logger)
        {
            _encryption = encryption;
            _logger = logger;
        }

        public Task<ConfirmationModel> Handle(DecodeConfirmationQuery request, CancellationToken cancellationToken)
        {
            try
            {
                return Task.FromResult(_encryption.DecryptFromString<ConfirmationModel>(request.Token));
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Confirmation manager format exception");
                return Task.FromException<ConfirmationModel>(
                    new ElwarkCryptographyException(CryptographyError.InvalidFormat, ex)
                );
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