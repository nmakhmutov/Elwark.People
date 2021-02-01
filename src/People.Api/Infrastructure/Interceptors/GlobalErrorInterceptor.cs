using System;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Grpc.Core;
using Grpc.Core.Interceptors;
using People.Domain.Exceptions;

namespace People.Api.Infrastructure.Interceptors
{
    public class GlobalErrorInterceptor : Interceptor
    {
        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request,
            ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                return await continuation(request, context);
            }
            catch (ElwarkException ex)
            {
                throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Code));
            }
            catch (ArgumentNullException ex)
            {
                var meta = new Metadata
                {
                    new($"field-{ex.ParamName}", ex.Message)
                };

                throw new RpcException(new Status(StatusCode.InvalidArgument, "InvalidModelState"), meta);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                var meta = new Metadata
                {
                    new($"field-{ex.ParamName}", ex.Message)
                };

                throw new RpcException(new Status(StatusCode.InvalidArgument, "InvalidModelState"), meta);
            }
            catch (ValidationException ex)
            {
                var result = ex.Errors.GroupBy(x => x.PropertyName)
                    .ToDictionary(x => $"field-{x.Key}", x => x.Select(t => t.ErrorCode));

                var meta = new Metadata();
                foreach (var (key, value) in result)
                    meta.Add(key, string.Join("|", value));

                throw new RpcException(new Status(StatusCode.InvalidArgument, "InvalidModelState"), meta);
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, "Unknown", ex));
            }
        }
    }
}