using Mediator;
using Microsoft.EntityFrameworkCore;
using People.Infrastructure;
using Testcontainers.PostgreSql;
using Xunit;

namespace People.IntegrationTests.Infrastructure;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public string ConnectionString =>
        _container?.GetConnectionString()
        ?? throw new InvalidOperationException("PostgreSQL container is not started.");

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();

        await _container.StartAsync();

        await using var ctx = CreateContext(new NoOpMediator());
        await ctx.Database.MigrateAsync();
    }

    public PeopleDbContext CreateContext(IMediator mediator, TimeProvider? timeProvider = null) =>
        new(
            new DbContextOptionsBuilder<PeopleDbContext>()
                .UseNpgsql(
                    ConnectionString,
                    npgsql => npgsql.ConfigureDataSource(ds => ds.EnableDynamicJson())
                )
                .Options,
            mediator,
            timeProvider ?? TimeProvider.System);

    public async Task DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }
}
