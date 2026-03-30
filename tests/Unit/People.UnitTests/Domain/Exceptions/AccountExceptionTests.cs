using People.Domain.Entities;
using People.Domain.Exceptions;
using Xunit;

namespace People.UnitTests.Domain.Exceptions;

public sealed class AccountExceptionTests
{
    [Fact]
    public void NotFound_SetsNameCodeIdMessage()
    {
        var id = new AccountId(42L);
        var ex = AccountException.NotFound(id);

        Assert.Equal(nameof(AccountException), ex.Name);
        Assert.Equal(nameof(AccountException.NotFound), ex.Code);
        Assert.Equal(id, ex.Id);
        Assert.Contains("42", ex.Message);
    }

    [Fact]
    public void PrimaryEmailCannotBeRemoved_SetsNameCodeIdMessage()
    {
        var id = new AccountId(7L);
        var ex = AccountException.PrimaryEmailCannotBeRemoved(id);

        Assert.Equal(nameof(AccountException), ex.Name);
        Assert.Equal(nameof(AccountException.PrimaryEmailCannotBeRemoved), ex.Code);
        Assert.Equal(id, ex.Id);
        Assert.Contains("primary", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
