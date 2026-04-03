using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Integration.Shared.Tests.Infrastructure;

namespace Integration.Worker.Tests.Health;

public sealed class WorkerHostFactory(PostgreSqlFixture postgres, string? connectionString = null) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Postgresql", connectionString ?? postgres.ConnectionString);
        builder.UseSetting("ConnectionStrings:Kafka", "127.0.0.1:19092");
    }
}
