using Microsoft.Extensions.Configuration;
using People.Api.Infrastructure;
using Xunit;

namespace People.UnitTests.Infrastructure;

public sealed class ConfigurationExtensionsTests
{
    [Fact]
    public void GetUri_ReturnsAbsoluteUri()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BaseUrl"] = "https://api.example.com/"
            })
            .Build();

        var uri = configuration.GetUri("BaseUrl");

        Assert.Equal(new Uri("https://api.example.com/"), uri);
    }

    [Fact]
    public void GetUri_WhenMissing_ThrowsInvalidOperationException()
    {
        var configuration = new ConfigurationBuilder().Build();

        var ex = Assert.Throws<InvalidOperationException>(() => configuration.GetUri("Missing"));

        Assert.Equal("Configuration value 'Missing' is required.", ex.Message);
    }

    [Fact]
    public void GetUri_WhenNotAbsolute_ThrowsInvalidOperationException()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BaseUrl"] = "not-a-uri"
            })
            .Build();

        var ex = Assert.Throws<InvalidOperationException>(() => configuration.GetUri("BaseUrl"));

        Assert.Equal("Configuration value 'BaseUrl' must be a valid absolute URI.", ex.Message);
    }
}
