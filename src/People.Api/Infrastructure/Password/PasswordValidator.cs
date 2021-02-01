using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using People.Domain.Exceptions;
using People.Infrastructure.Forbidden;

namespace People.Api.Infrastructure.Password
{
    public class PasswordValidator: IPasswordValidator
    {
        private readonly IForbiddenService _forbidden;
        private readonly PasswordValidationOptions _options;

        public PasswordValidator(IOptions<PasswordValidationOptions> settings, IForbiddenService forbidden)
        {
            _forbidden = forbidden;
            _options = settings.Value ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task ValidateAsync(string password, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ElwarkException(ElwarkExceptionCodes.PasswordEmpty);

            if (password.Length < _options.RequiredLength)
                throw new ElwarkException(ElwarkExceptionCodes.PasswordTooShort);

            if (_options.RequireNonAlphanumeric && password.All(IsLetterOrDigit))
                throw new ElwarkException(ElwarkExceptionCodes.PasswordRequiresNonAlphanumeric);

            if (_options.RequireDigit && !password.Any(IsDigit))
                throw new ElwarkException(ElwarkExceptionCodes.PasswordRequiresDigit);

            if (_options.RequireLowercase && !password.Any(IsLower))
                throw new ElwarkException(ElwarkExceptionCodes.PasswordRequiresLower);

            if (_options.RequireUppercase && !password.Any(IsUpper))
                throw new ElwarkException(ElwarkExceptionCodes.PasswordRequiresUpper);

            if (_options.RequiredUniqueChars > 1 && password.Distinct().Count() <= _options.RequiredUniqueChars)
                throw new ElwarkException(ElwarkExceptionCodes.PasswordRequiresUniqueChars);
            
            if (await _forbidden.IsPasswordForbidden(password, ct))
                throw new ElwarkException(ElwarkExceptionCodes.PasswordForbidden);
        }
        
        private static bool IsDigit(char c) =>
            c >= '0' && c <= '9';

        private static bool IsLower(char c) =>
            c >= 'a' && c <= 'z';

        private static bool IsUpper(char c) =>
            c >= 'A' && c <= 'Z';

        private static bool IsLetterOrDigit(char c) =>
            IsUpper(c) || IsLower(c) || IsDigit(c);
    }
}