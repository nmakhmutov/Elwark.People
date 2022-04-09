using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using People.Domain.Exceptions;
using People.Infrastructure.Blacklist;

namespace People.Api.Infrastructure.Password;

public sealed class PasswordValidator : IPasswordValidator
{
    private readonly IBlacklistService _blacklist;
    private readonly PasswordValidationOptions _options;

    public PasswordValidator(IOptions<PasswordValidationOptions> settings, IBlacklistService blacklist)
    {
        _blacklist = blacklist;
        _options = settings.Value ?? throw new ArgumentNullException(nameof(settings));
    }

    public async Task<PasswordResult> ValidateAsync(string password, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(password))
            return PasswordResult.Fail(ExceptionCodes.PasswordEmpty);

        if (password.Length < _options.RequiredLength)
            return PasswordResult.Fail(ExceptionCodes.PasswordTooShort);

        if (_options.RequireNonAlphanumeric && password.All(IsLetterOrDigit))
            return PasswordResult.Fail(ExceptionCodes.PasswordRequiresNonAlphanumeric);

        if (_options.RequireDigit && !password.Any(IsDigit))
            return PasswordResult.Fail(ExceptionCodes.PasswordRequiresDigit);

        if (_options.RequireLowercase && !password.Any(IsLower))
            return PasswordResult.Fail(ExceptionCodes.PasswordRequiresLower);

        if (_options.RequireUppercase && !password.Any(IsUpper))
            return PasswordResult.Fail(ExceptionCodes.PasswordRequiresUpper);

        if (_options.RequiredUniqueChars > 1 && password.Distinct().Count() <= _options.RequiredUniqueChars)
            return PasswordResult.Fail(ExceptionCodes.PasswordRequiresUniqueChars);

        if (await _blacklist.IsPasswordForbidden(password, ct))
            return PasswordResult.Fail(ExceptionCodes.PasswordForbidden);

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
