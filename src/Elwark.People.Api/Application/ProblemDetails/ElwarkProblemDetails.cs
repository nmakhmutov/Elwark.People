using Elwark.People.Api.Infrastructure;
using Elwark.People.Domain.ErrorCodes;
using Microsoft.AspNetCore.Http;

namespace Elwark.People.Api.Application.ProblemDetails
{
    public class ElwarkProblemDetails : Microsoft.AspNetCore.Mvc.ProblemDetails
    {
        public static ElwarkProblemDetails Unknown => new ElwarkProblemDetails
        {
            Title = CommonError.Unknown.ToString(),
            Type = CommonError.Unknown.ToString(),
            Status = StatusCodes.Status503ServiceUnavailable,
            Detail = ErrorMessageResources.CommonError_Unknown
        };
        
        public static ElwarkProblemDetails Internal => new ElwarkProblemDetails
        {
            Title = CommonError.Internal.ToString(),
            Type = CommonError.Internal.ToString(),
            Status = StatusCodes.Status500InternalServerError,
            Detail = ErrorMessageResources.CommonError_Internal
        };

        public static ElwarkProblemDetails NotSupported(string message) =>
            new ElwarkProblemDetails
            {
                Title = CommonError.NotSupported.ToString(),
                Type = CommonError.NotSupported.ToString(),
                Status = StatusCodes.Status400BadRequest,
                Detail = message
            };
    }
}