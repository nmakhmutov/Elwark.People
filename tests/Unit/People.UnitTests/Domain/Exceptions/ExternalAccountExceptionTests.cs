using People.Domain.Entities;
using People.Domain.Exceptions;
using Xunit;

namespace People.UnitTests.Domain.Exceptions;

public sealed class ExternalAccountExceptionTests
{
    [Fact]
    public void NotFound_SetsServiceIdentityCodeMessage()
    {
        var ex = ExternalAccountException.NotFound(ExternalService.Google, "g-123");

        Assert.Equal(nameof(ExternalAccountException), ex.Name);
        Assert.Equal(nameof(ExternalAccountException.NotFound), ex.Code);
        Assert.Equal(ExternalService.Google, ex.Service);
        Assert.Equal("g-123", ex.Identity);
        Assert.Contains("Google", ex.Message);
        Assert.Contains("g-123", ex.Message);
    }

    [Fact]
    public void AlreadyCreated_SetsServiceIdentityCodeMessage()
    {
        var ex = ExternalAccountException.AlreadyCreated(ExternalService.Microsoft, "m-456");

        Assert.Equal(nameof(ExternalAccountException), ex.Name);
        Assert.Equal(nameof(ExternalAccountException.AlreadyCreated), ex.Code);
        Assert.Equal(ExternalService.Microsoft, ex.Service);
        Assert.Equal("m-456", ex.Identity);
        Assert.Contains("Microsoft", ex.Message);
        Assert.Contains("m-456", ex.Message);
    }
}
