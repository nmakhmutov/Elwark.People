using System.Runtime.CompilerServices;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using People.Domain.Entities;
using People.Domain.SeedWork;
using People.Infrastructure.EntityConfigurations;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
namespace People.Infrastructure;

public sealed class PeopleDbContext : DbContext,
    IUnitOfWork
{
    private readonly IMediator _mediator;
    private readonly TimeProvider _timeProvider;

    public PeopleDbContext(DbContextOptions<PeopleDbContext> options, IMediator mediator, TimeProvider timeProvider)
        : base(options)
    {
        _mediator = mediator;
        _timeProvider = timeProvider;
    }

    public DbSet<Account> Accounts =>
        Set<Account>();

    public DbSet<EmailAccount> Emails =>
        Set<EmailAccount>();

    public DbSet<ExternalConnection> Connections =>
        Set<ExternalConnection>();

    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken)
    {
        foreach (var entry in ChangeTracker.Entries<IAggregateRoot>())
            entry.Entity.SetAsUpdated(_timeProvider);

        await SaveChangesAsync(cancellationToken);

        await _mediator.DispatchDomainEventsAsync(this);

        return true;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AccountEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new EmailEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new ExternalConnectionEntityTypeConfiguration());

        modelBuilder.ApplyConfiguration(new ConfirmationEntityTypeConfiguration());
    }
}

public sealed class OrderingContextDesignFactory : IDesignTimeDbContextFactory<PeopleDbContext>
{
    public PeopleDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PeopleDbContext>()
            .UseNpgsql("Host=_;Database=_;Username=_;Password=_");

        return new PeopleDbContext(optionsBuilder.Options, new NoMediator(), TimeProvider.System);
    }

    private sealed class NoMediator : IMediator
    {
        public async IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            [EnumeratorCancellation] CancellationToken ct
        )
        {
            await Task.Yield();
            yield break;
        }

        public async IAsyncEnumerable<object?> CreateStream(
            object request,
            [EnumeratorCancellation] CancellationToken ct
        )
        {
            await Task.Yield();
            yield break;
        }

        public async IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamQuery<TResponse> query,
            [EnumeratorCancellation] CancellationToken ct
        )
        {
            await Task.Yield();
            yield break;
        }

        public async IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamCommand<TResponse> command,
            [EnumeratorCancellation] CancellationToken ct
        )
        {
            await Task.Yield();
            yield break;
        }

        public ValueTask Publish<TNotification>(TNotification notification, CancellationToken ct)
            where TNotification : INotification =>
            ValueTask.CompletedTask;

        public ValueTask Publish(object notification, CancellationToken ct) =>
            ValueTask.CompletedTask;

        public ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct) =>
            ValueTask.FromResult<TResponse>(default!);

        public ValueTask<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct) =>
            ValueTask.FromResult<TResponse>(default!);

        public ValueTask<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken ct) =>
            ValueTask.FromResult<TResponse>(default!);

        public ValueTask<object?> Send(object request, CancellationToken ct) =>
            ValueTask.FromResult<object?>(null);
    }
}
