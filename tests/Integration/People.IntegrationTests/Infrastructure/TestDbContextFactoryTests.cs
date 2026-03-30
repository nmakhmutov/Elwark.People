using Xunit;

namespace People.IntegrationTests.Infrastructure;

public sealed class TestDbContextFactoryTests
{
    [Fact]
    public void CreateNoOpBus_ReturnsNotNull() =>
        Assert.NotNull(TestDbContextFactory.CreateNoOpIntegrationEventBus());
}
