using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using People.Domain.AggregatesModel.AccountAggregate;
using People.Domain.SeedWork;
using People.Infrastructure.EntityConfigurations;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace People.Infrastructure;

public sealed class PeopleDbContext : DbContext,
    IUnitOfWork
{
    private readonly IMediator _mediator;

    public PeopleDbContext(DbContextOptions<PeopleDbContext> options, IMediator mediator)
        : base(options) =>
        _mediator = mediator;

    public DbSet<Account> Accounts { get; set; } = default!;

    public DbSet<EmailAccount> Emails { get; set; } = default!;

    public DbSet<ExternalConnection> Connections { get; set; } = default!;

    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken)
    {
        await SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
        
        await _mediator
            .DispatchDomainEventsAsync(this)
            .ConfigureAwait(false);

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

        return new PeopleDbContext(optionsBuilder.Options, new NoMediator());
    }

    private sealed class NoMediator : IMediator
    {
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request,
            CancellationToken ct) =>
            default!;

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken ct) =>
            default!;

        public Task Publish<TNotification>(TNotification notification, CancellationToken ct)
            where TNotification : INotification =>
            Task.CompletedTask;

        public Task Publish(object notification, CancellationToken ct) =>
            Task.CompletedTask;

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct) =>
            Task.FromResult<TResponse>(default!);

        public Task Send<TRequest>(TRequest request, CancellationToken ct)
            where TRequest : IRequest =>
            Task.CompletedTask;

        public Task<object?> Send(object request, CancellationToken ct) =>
            Task.FromResult<object?>(null);
    }
}
