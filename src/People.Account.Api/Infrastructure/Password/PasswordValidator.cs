using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using People.Account.Infrastructure.Forbidden;
using People.Domain.Exceptions;

namespace People.Account.Api.Infrastructure.Password
{
    public class PasswordValidator : IPasswordValidator
    {
        private readonly IForbiddenService _forbidden;
        private readonly PasswordValidationOptions _options;

        public PasswordValidator(IOptions<PasswordValidationOptions> settings, IForbiddenService forbidden)
        {
            _forbidden = forbidden;
            _options = settings.Value ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task<PasswordResult> ValidateAsync(string password, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(password))
                return PasswordResult.Fail(ElwarkExceptionCodes.PasswordEmpty);

            if (password.Length < _options.RequiredLength)
                return PasswordResult.Fail(ElwarkExceptionCodes.PasswordTooShort);

            if (_options.RequireNonAlphanumeric && password.All(IsLetterOrDigit))
                return PasswordResult.Fail(ElwarkExceptionCodes.PasswordRequiresNonAlphanumeric);

            if (_options.RequireDigit && !password.Any(IsDigit))
                return PasswordResult.Fail(ElwarkExceptionCodes.PasswordRequiresDigit);

            if (_options.RequireLowercase && !password.Any(IsLower))
                return PasswordResult.Fail(ElwarkExceptionCodes.PasswordRequiresLower);

            if (_options.RequireUppercase && !password.Any(IsUpper))
                return PasswordResult.Fail(ElwarkExceptionCodes.PasswordRequiresUpper);

            if (_options.RequiredUniqueChars > 1 && password.Distinct().Count() <= _options.RequiredUniqueChars)
                return PasswordResult.Fail(ElwarkExceptionCodes.PasswordRequiresUniqueChars);

            if (await _forbidden.IsPasswordForbidden(password, ct))
                return PasswordResult.Fail(ElwarkExceptionCodes.PasswordForbidden);
            
            return PasswordResult.Success();
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