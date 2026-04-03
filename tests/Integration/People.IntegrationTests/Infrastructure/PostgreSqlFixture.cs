using Microsoft.EntityFrameworkCore;
using People.Infrastructure;
using People.Infrastructure.Mappers;
using People.Infrastructure.Outbox;
using Testcontainers.PostgreSql;
using Xunit;

namespace People.IntegrationTests.Infrastructure;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18")
        .Build();

    public string ConnectionString =>
        _container.GetConnectionString()
        ?? throw new InvalidOperationException("PostgreSQL container is not started.");

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var ctx = CreateContext();
        await ctx.Database.MigrateAsync();
    }

    public PeopleDbContext CreateContext(TimeProvider? timeProvider = null)
    {
        var options = new DbContextOptionsBuilder<PeopleDbContext>()
            .UseNpgsql(ConnectionString, x => x.ConfigureDataSource(builder => builder.EnableDynamicJson()))
            .Options;

        var pipeline = new OutboxPipeline<PeopleDbContext>(
            new OutboxMapperRegistry<PeopleDbContext>()
                .AddMapper(new AccountCreatedMapper())
                .AddMapper(new AccountUpdatedMapper())
                .AddMapper(new AccountDeletedMapper())
        );

        return new PeopleDbContext(options, pipeline, timeProvider ?? TimeProvider.System);
    }

    public async Task DisposeAsync() =>
        await _container.DisposeAsync();
}
