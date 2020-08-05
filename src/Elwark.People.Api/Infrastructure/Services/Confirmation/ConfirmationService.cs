using System;
using System.Security.Cryptography;
using System.Text;
using Elwark.People.Abstractions;
using Elwark.People.Api.Infrastructure.Security;
using Elwark.People.Domain.ErrorCodes;
using Elwark.People.Domain.Exceptions;
using Elwark.People.Shared.Primitives;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Elwark.People.Api.Infrastructure.Services.Confirmation
{
    public class ConfirmationService : IConfirmationService
    {
        private readonly IDataEncryption _encryption;
        private readonly ILogger<ConfirmationService> _logger;

        public ConfirmationService(IDataEncryption encryption, ILogger<ConfirmationService> logger)
        {
            _encryption = encryption;
            _logger = logger;
        }

        public string WriteToken(Guid confirmationId, IdentityId identityId, ConfirmationType type, long code) =>
            _encryption.EncryptToString(new ConfirmationData(confirmationId, identityId, type, code));

        public ConfirmationData ReadToken(string token)
        {
            try
            {
                return _encryption.DecryptFromString<ConfirmationData>(token);
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Confirmation manager format exception");
                throw new ElwarkCryptographyException(CryptographyError.InvalidFormat, ex);
            }
            catch (DecoderFallbackException ex)
            {
                _logger.LogError(ex, "Confirmation manager decode fallback exception");
                throw new ElwarkCryptographyException(CryptographyError.DecoderError, ex);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Confirmation manager json exception");
                throw new ElwarkCryptographyException(CryptographyError.JsonError, ex);
            }
            catch (CryptographicException ex)
            {
                _logger.LogError(ex, "Confirmation manager cryptographic exception");
                throw new ElwarkCryptographyException(CryptographyError.DecoderError, ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Confirmation manager unknown exception");
                throw new ElwarkCryptographyException(CryptographyError.UnknownError, ex);
            }
        }
    }
}