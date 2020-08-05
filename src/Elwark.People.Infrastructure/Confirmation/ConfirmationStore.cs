using System;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Elwark.People.Infrastructure.Confirmation
{
    public class ConfirmationStore : IConfirmationStore
    {
        private readonly OAuthContext _context;

        public ConfirmationStore(OAuthContext context) =>
            _context = context ?? throw new ArgumentNullException(nameof(context));

        public Task<ConfirmationModel> GetAsync(Guid id, CancellationToken cancellationToken) =>
            _context.Set<ConfirmationModel>()
                .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        public Task<ConfirmationModel> GetAsync(IdentityId id, long code, CancellationToken cancellationToken) =>
            _context.Set<ConfirmationModel>()
                .SingleOrDefaultAsync(x => x.IdentityId == id && x.Code == code, cancellationToken);

        public async Task<ConfirmationModel> CreateAsync(ConfirmationModel confirmation,
            CancellationToken cancellationToken)
        {
            var result = await _context.AddAsync(confirmation, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            return result.Entity;
        }

        public Task DeleteAsync(Guid id, CancellationToken cancellationToken) =>
            _context.Database.ExecuteSqlRawAsync(
                $"DELETE FROM confirmations WHERE id = '{id}'",
                cancellationToken
            );
    }
}