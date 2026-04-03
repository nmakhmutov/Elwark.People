using Grpc.Core;
using Grpc.Core.Interceptors;
using Mediator;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using People.Api.Grpc;
using People.Api.Infrastructure.Interceptors;
using Integration.Api.Tests.Queries;
using GrpcPeopleBase = People.Grpc.People.PeopleService.PeopleServiceBase;

namespace Integration.Api.Tests.Grpc;

/// <summary>
/// gRPC <see cref="PeopleService"/> tests: shared DB fixture and helpers to run unary RPCs through <see cref="GrpcExceptionInterceptor"/>.
/// </summary>
public abstract class GrpcPeopleServiceTestBase : QueryIntegrationTestBase
{
    protected GrpcPeopleServiceTestBase(PostgreSqlFixture postgres)
        : base(postgres)
    {
    }

    protected static ServerCallContext CreateCallContext(CancellationToken cancellationToken = default)
    {
        var ctx = Substitute.For<ServerCallContext>();
        ctx.CancellationToken.Returns(cancellationToken);
        ctx.Method.Returns("/people.People/Unary");
        return ctx;
    }

    protected static Interceptor CreateInterceptor() =>
        new GrpcExceptionInterceptor(NullLoggerFactory.Instance);

    protected static GrpcPeopleBase CreatePeopleService(IMediator mediator) =>
        new PeopleService(mediator);

    /// <summary>Invokes a <see cref="PeopleService"/> unary method through the gRPC exception interceptor.</summary>
    protected static Task<TResponse> InterceptUnaryAsync<TRequest, TResponse>(
        Interceptor interceptor,
        GrpcPeopleBase service,
        TRequest request,
        Func<GrpcPeopleBase, TRequest, ServerCallContext, Task<TResponse>> invoke,
        CancellationToken cancellationToken = default
    )
        where TRequest : class
        where TResponse : class
    {
        var context = CreateCallContext(cancellationToken);
        return interceptor.UnaryServerHandler(
            request,
            context,
            async (req, ctx) => await invoke(service, req, ctx));
    }
}
