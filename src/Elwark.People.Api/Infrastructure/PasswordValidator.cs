using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Api.Settings;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using Elwark.People.Domain.ErrorCodes;
using Elwark.People.Domain.Exceptions;
using Elwark.Storage.Client;
using Microsoft.Extensions.Options;

namespace Elwark.People.Api.Infrastructure
{
    public class PasswordValidator: IPasswordValidator
    {
        private readonly IElwarkStorageClient _client;
        private readonly PasswordSettings _settings;

        public PasswordValidator(IElwarkStorageClient client, IOptions<PasswordSettings> settings)
        {
            _client = client;
            _settings = settings.Value ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task ValidateAsync(string password, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ElwarkPasswordException(PasswordError.Empty);

            if (password.Length < _settings.RequiredLength)
                throw new ElwarkPasswordException(PasswordError.TooShort);

            if (_settings.RequireNonAlphanumeric && password.All(IsLetterOrDigit))
                throw new ElwarkPasswordException(PasswordError.RequiresNonAlphanumeric);

            if (_settings.RequireDigit && !password.Any(IsDigit))
                throw new ElwarkPasswordException(PasswordError.RequiresDigit);

            if (_settings.RequireLowercase && !password.Any(IsLower))
                throw new ElwarkPasswordException(PasswordError.RequiresLower);

            if (_settings.RequireUppercase && !password.Any(IsUpper))
                throw new ElwarkPasswordException(PasswordError.RequiresUpper);

            if (_settings.RequiredUniqueChars > 1 && password.Distinct().Count() <= _settings.RequiredUniqueChars)
                throw new ElwarkPasswordException(PasswordError.RequiresUniqueChars);
            
            if (await _client.Blacklist.IsForbiddenPasswordAsync(password, ct))
                throw new ElwarkPasswordException(PasswordError.Worst);
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