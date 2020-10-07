using System;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using Elwark.People.Domain.SeedWork;
using Elwark.People.Infrastructure.EntityConfigurations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Elwark.People.Infrastructure
{
    public class OAuthContext : DbContext, IUnitOfWork
    {
        private readonly IMediator _mediator;

        public OAuthContext(DbContextOptions<OAuthContext> options)
            : base(options) =>
            _mediator = new FakeMediator();

        public OAuthContext(DbContextOptions<OAuthContext> options, IMediator mediator)
            : base(options) =>
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

        public DbSet<Account> Accounts { get; set; } = default!;

        public DbSet<Identity> Identities { get; set; } = default!;

        public virtual async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
        {
            await SaveChangesAsync(cancellationToken);
            await _mediator.DispatchDomainEventsAsync(this, cancellationToken);

            return true;
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.HasPostgresExtension("uuid-ossp");

            builder.ApplyConfiguration(new AccountEntityTypeConfiguration());
            builder.ApplyConfiguration(new IdentityEntityTypeConfiguration());
        }

        private class FakeMediator : IMediator
        {
            public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken)
                where TNotification : INotification =>
                Task.CompletedTask;

            public Task<object?> Send(object request, CancellationToken cancellationToken) =>
                Task.FromResult<object?>(default!);

            public Task Publish(object notification, CancellationToken cancellationToken) =>
                Task.CompletedTask;

            public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken) =>
                Task.FromResult<TResponse>(default!);
        }
    }
}