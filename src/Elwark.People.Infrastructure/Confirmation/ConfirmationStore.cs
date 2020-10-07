using System;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Infrastructure.Cache;
using Elwark.People.Shared.Primitives;

namespace Elwark.People.Infrastructure.Confirmation
{
    public class ConfirmationStore : IConfirmationStore
    {
        private readonly ICacheStorage _cache;

        public ConfirmationStore(ICacheStorage cache) =>
            _cache = cache;

        public Task<ConfirmationModel?> GetAsync(IdentityId id, ConfirmationType type) =>
            _cache.ReadAsync<ConfirmationModel>(CreateCacheKey(id, type));

        public Task DeleteAsync(IdentityId id, ConfirmationType type) =>
            _cache.DeleteAsync(CreateCacheKey(id, type));

        public Task<bool> CreateAsync(ConfirmationModel confirmation, TimeSpan expiry) =>
            _cache.CreateAsync(CreateCacheKey(confirmation.IdentityId, confirmation.Type), confirmation,expiry);

        private static string CreateCacheKey(IdentityId id, ConfirmationType type) =>
            $"{nameof(ConfirmationStore)}.{type}.{id}";
    }
}