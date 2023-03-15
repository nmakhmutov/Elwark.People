using FluentValidation;
using Grpc.Core;
using Grpc.Core.Interceptors;
using People.Domain.Exceptions;

namespace People.Api.Infrastructure.Interceptors;

internal sealed class GrpcExceptionInterceptor : Interceptor
{
    private readonly ILogger<GrpcExceptionInterceptor> _logger;

    public GrpcExceptionInterceptor(ILoggerFactory factory) =>
        _logger = factory.CreateLogger<GrpcExceptionInterceptor>();

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request,
        ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context)
                .ConfigureAwait(false);
        }
        catch (PeopleException ex)
        {
            throw WrapPeopleException(context, ex);
        }
        catch (ValidationException ex)
        {
            throw WrapValidationException(context, ex);
        }
        catch (ArgumentException ex)
        {
            throw WrapArgumentExceptions(context, ex);
        }
        catch (Exception ex)
        {
            throw WrapUnhandledException(context, ex);
        }
    }

    private RpcException WrapPeopleException(ServerCallContext context, PeopleException ex)
    {
        _logger.LogError("Exception {Name} occured at the endpoint {Method}", ex.Name, context.Method);

        var code = ex.Code switch
        {
            "NotFound" => StatusCode.NotFound,
            "Forbidden" => StatusCode.PermissionDenied,
            _ => StatusCode.FailedPrecondition
        };

        var meta = new Metadata
        {
            { "ex-name", ex.Name },
            { "ex-code", ex.Code },
            {
                "ex-id", ex switch
                {
                    AccountException x => x.Id.ToString(),
                    EmailException x => x.Email.Address,
                    ExternalAccountException x => $"{x.Service}:{x.Identity}",
                    _ => string.Empty
                }
            }
        };

        throw new RpcException(new Status(code, ex.Message), meta);
    }

    private RpcException WrapValidationException(ServerCallContext context, ValidationException ex)
    {
        const string name = nameof(ValidationException);
        _logger.LogWarning(ex, "Exception {Name} occured at the endpoint {Method}", name, context.Method);

        var meta = new Metadata
        {
            { "ex-name", name },
            { "ex-code", "InvalidModel" }
        };

        var result = ex.Errors.GroupBy(x => x.PropertyName)
            .ToDictionary(x => $"ex-field-{x.Key}", x => x.Select(t => t.ErrorCode));

        foreach (var (field, errors) in result)
            meta.Add(field, string.Join("|", errors));

        throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message), meta);
    }

    private RpcException WrapArgumentExceptions(ServerCallContext context, ArgumentException ex)
    {
        const string name = nameof(ArgumentException);
        _logger.LogCritical(ex, "Exception {Name} occured at the endpoint {Method}", name, context.Method);

        var meta = new Metadata
        {
            { "ex-name", name },
            { "ex-code", "InvalidModel" },
            { $"ex-field-{ex.ParamName}", ex.Message }
        };

        throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message), meta);
    }

    private RpcException WrapUnhandledException(ServerCallContext context, Exception ex)
    {
        var name = ex.GetType().Name;
        _logger.LogCritical(ex, "Unhandled exception {Name} occured at the endpoint {Method}", name, context.Method);

        var meta = new Metadata
        {
            { "ex-name", name },
            { "ex-code", "Unhandled" }
        };

        throw new RpcException(new Status(StatusCode.Internal, ex.Message), meta);
    }
}
