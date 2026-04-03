using System.Net.Mail;
using People.Domain.Exceptions;
using Xunit;

namespace Unit.Api.Tests.Domain.Exceptions;

public sealed class EmailExceptionTests
{
    [Fact]
    public void NotFound_ContainsEmailInMessage()
    {
        var addr = new MailAddress("missing@x.com");
        var ex = EmailException.NotFound(addr);

        Assert.Equal(nameof(EmailException), ex.Name);
        Assert.Equal(nameof(EmailException.NotFound), ex.Code);
        Assert.Equal(addr.Address, ex.Email.Address);
        Assert.Contains("missing@x.com", ex.Message);
    }

    [Fact]
    public void AlreadyCreated_HasCodeAndEmail()
    {
        var addr = new MailAddress("dup@x.com");
        var ex = EmailException.AlreadyCreated(addr);

        Assert.Equal(nameof(EmailException.AlreadyCreated), ex.Code);
        Assert.Equal(addr.Address, ex.Email.Address);
        Assert.Contains("dup@x.com", ex.Message);
    }

    [Fact]
    public void NotConfirmed_HasCodeAndEmail()
    {
        var addr = new MailAddress("wait@x.com");
        var ex = EmailException.NotConfirmed(addr);

        Assert.Equal(nameof(EmailException.NotConfirmed), ex.Code);
        Assert.Equal(addr.Address, ex.Email.Address);
        Assert.Contains("wait@x.com", ex.Message);
    }

    [Fact]
    public void AlreadyConfirmed_HasCodeAndEmail()
    {
        var addr = new MailAddress("ok@x.com");
        var ex = EmailException.AlreadyConfirmed(addr);

        Assert.Equal(nameof(EmailException.AlreadyConfirmed), ex.Code);
        Assert.Equal(addr.Address, ex.Email.Address);
        Assert.Contains("ok@x.com", ex.Message);
    }
}
