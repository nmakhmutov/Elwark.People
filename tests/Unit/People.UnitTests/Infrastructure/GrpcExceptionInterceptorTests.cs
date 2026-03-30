using System.Net.Mail;
using Grpc.Core;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using People.Api.Infrastructure.Interceptors;
using People.Domain.Entities;
using People.Domain.Exceptions;
using Xunit;

namespace People.UnitTests.Infrastructure;

public sealed class GrpcExceptionInterceptorTests
{
    private static GrpcExceptionInterceptor CreateInterceptor() =>
        new(NullLoggerFactory.Instance);

    private static ServerCallContext CreateContext()
    {
        var context = Substitute.For<ServerCallContext>();
        context.Method.Returns("/people.People/Test");
        return context;
    }

    [Theory]
    [InlineData("NotFound", StatusCode.NotFound)]
    [InlineData("Forbidden", StatusCode.PermissionDenied)]
    [InlineData("Other", StatusCode.FailedPrecondition)]
    public async Task PeopleException_MapsToExpectedGrpcStatus(string code, StatusCode expected)
    {
        var interceptor = CreateInterceptor();
        var context = CreateContext();
        var ex = new TestPeopleException(code);

        var rpc = await Assert.ThrowsAsync<RpcException>(() =>
            interceptor.UnaryServerHandler(
                request: new object(),
                context,
                (_, _) => Task.FromException<object>(ex)));

        Assert.Equal(expected, rpc.Status.StatusCode);
    }

    [Fact]
    public async Task AccountException_NotFound_MapsToNotFound()
    {
        var interceptor = CreateInterceptor();
        var context = CreateContext();

        var rpc = await Assert.ThrowsAsync<RpcException>(() =>
            interceptor.UnaryServerHandler(
                request: new object(),
                context,
                (_, _) => Task.FromException<object>(AccountException.NotFound(new AccountId(9)))));

        Assert.Equal(StatusCode.NotFound, rpc.Status.StatusCode);
    }

    [Fact]
    public async Task EmailException_AlreadyCreated_MapsToAlreadyExists()
    {
        var interceptor = CreateInterceptor();
        var context = CreateContext();
        var email = new MailAddress("user@example.com");

        var rpc = await Assert.ThrowsAsync<RpcException>(() =>
            interceptor.UnaryServerHandler(
                request: new object(),
                context,
                (_, _) => Task.FromException<object>(EmailException.AlreadyCreated(email))));

        Assert.Equal(StatusCode.AlreadyExists, rpc.Status.StatusCode);
    }

    [Fact]
    public async Task UnknownException_MapsToInternal()
    {
        var interceptor = CreateInterceptor();
        var context = CreateContext();

        var rpc = await Assert.ThrowsAsync<RpcException>(() =>
            interceptor.UnaryServerHandler(
                request: new object(),
                context,
                (_, _) => Task.FromException<object>(new InvalidOperationException("boom"))));

        Assert.Equal(StatusCode.Internal, rpc.Status.StatusCode);
    }

    private sealed class TestPeopleException(string code) : PeopleException("TestPeople", code, "msg");
}
