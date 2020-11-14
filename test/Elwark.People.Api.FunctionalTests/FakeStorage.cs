using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Elwark.Storage.Client;
using Elwark.Storage.Client.Abstraction;

namespace Elwark.People.Api.FunctionalTests
{
    public class FakeStorage : IElwarkStorageClient
    {
        public ICountryEndpoint Country { get; } = default!;
        public IBlacklistEndpoint Blacklist { get; } = new BlacklistEndpoint();
        public ICurrencyEndpoint Currency { get; } = default!;
        public ILanguageEndpoint Language { get; } = default!;
        public ITimezoneEndpoint Timezone { get; } = default!;
        public IStaticEndpoint Static { get; } = new StaticEndpoint();
        public IImageEndpoint Images { get; } = default!;

        private class StaticEndpoint : IStaticEndpoint
        {
            public IIcons Icons { get; } = new IconsEndpoint();

            public class IconsEndpoint : IIcons
            {
                public IUserIcon User { get; } = new UserIconEndpoint();

                public IElwarkIcons Elwark { get; } = default!;

                private class UserIconEndpoint : IUserIcon
                {
                    public IImage Default { get; } = new ImageEndpoint();

                    private class ImageEndpoint : IImage
                    {
                        public Task<HttpResponseMessage> GetAsync(
                            CancellationToken cancellationToken = new CancellationToken()) =>
                            throw new NotImplementedException();

                        public Task<Stream> GetStreamAsync() =>
                            throw new NotImplementedException();

                        public Task<byte[]> GetBytesAsync() =>
                            throw new NotImplementedException();

                        public Uri Path { get; } = new Uri("http://testimage.com/random");
                    }
                }
            }
        }

        private class BlacklistEndpoint : IBlacklistEndpoint
        {
            public Task<IReadOnlyCollection<string>> GetPasswordsAsync(CancellationToken cancellationToken) =>
                throw new NotImplementedException();

            public Task<IReadOnlyCollection<string>> GetEmailDomainsAsync(CancellationToken cancellationToken) =>
                throw new NotImplementedException();

            public Task<bool> IsForbiddenPasswordAsync(string password, CancellationToken cancellationToken) =>
                Task.FromResult(password == "true");

            public Task<bool> IsForbiddenEmailDomainAsync(string domain, CancellationToken cancellationToken) =>
                Task.FromResult(domain == "true");
        }
    }
}