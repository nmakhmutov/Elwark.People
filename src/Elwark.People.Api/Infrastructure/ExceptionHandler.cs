using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Elwark.Extensions;
using Elwark.People.Api.Application.ProblemDetails;
using Elwark.People.Api.Error;
using Elwark.People.Domain.ErrorCodes;
using Elwark.People.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Elwark.People.Api.Infrastructure
{
    public static class ExceptionHandler
    {
        public static ValidationProblemDetails CreateProblemDetails(ModelStateDictionary state) =>
            new ValidationProblemDetails(state)
            {
                Title = nameof(CommonError),
                Type = nameof(CommonError.InvalidModelState),
                Status = StatusCodes.Status400BadRequest,
                Detail = GetDetails(GetKey(nameof(CommonError), CommonError.InvalidModelState.ToString()))
            };

        public static ProblemDetails CreateProblemDetails(Exception ex) =>
            ex switch
            {
                ElwarkAccountBlockedException x => Create(x),

                ElwarkAccountException x => Create(x),

                ElwarkConfirmationAlreadySentException x => Create(x),

                ElwarkIdentificationException x => Create(x),

                ElwarkNotificationException x => Create(x),

                ElwarkPasswordException x => Create(x),

                ElwarkConfirmationException x => Create(x),

                ElwarkCryptographyException x => Create(x),

                ElwarkModelStateException x => Create(x),

                ElwarkException x => Create(x),

                ArgumentNullException x => Create(x.ParamName, "Cannot be empty"),

                ArgumentException x => Create(x.ParamName, x.Message),

                NotSupportedException x => ElwarkProblemDetails.NotSupported(x.Message),

                _ => ElwarkProblemDetails.Internal
            };

        private static AccountBlockedProblemDetails Create(ElwarkAccountBlockedException ex) =>
            new AccountBlockedProblemDetails
            {
                Title = ex.Group,
                Type = ex.Type,
                Instance = ex.Source,
                Detail = GetDetails(GetKey(ex), ex.AccountId, ex.BanType, ex.ExpiredAt?.ToString("G"), ex.Reason),
                Status = StatusCodes.Status403Forbidden,

                AccountId = ex.AccountId,
                BanType = ex.BanType,
                ExpiredAt = ex.ExpiredAt,
                Reason = ex.Reason
            };

        private static AccountProblemDetails Create(ElwarkAccountException ex) =>
            new AccountProblemDetails
            {
                Title = ex.Group,
                Type = ex.Type,
                Instance = ex.Source,
                Detail = GetDetails(GetKey(ex), ex.AccountId),
                Status = ex.Code switch
                {
                    AccountError.NotFound => StatusCodes.Status404NotFound,
                    _ => StatusCodes.Status400BadRequest
                },

                AccountId = ex.AccountId
            };

        private static IdentificationProblemDetails Create(ElwarkIdentificationException ex) =>
            new IdentificationProblemDetails
            {
                Title = ex.Group,
                Type = ex.Type,
                Instance = ex.Source,
                Detail = GetDetails(GetKey(ex), ex.Identifier?.Type, ex.Identifier?.Value),
                Status = ex.Code switch
                {
                    IdentificationError.NotFound => StatusCodes.Status404NotFound,
                    _ => StatusCodes.Status400BadRequest
                },
                Identifier = ex.Identifier
            };

        private static NotificationProblemDetails Create(ElwarkNotificationException ex) =>
            new NotificationProblemDetails
            {
                Title = ex.Group,
                Type = ex.Type,
                Instance = ex.Source,
                Detail = GetDetails(GetKey(ex), ex.Notifier?.Type, ex.Notifier?.Value),
                Status = ex.Code switch
                {
                    NotificationError.NotFound => StatusCodes.Status404NotFound,
                    _ => StatusCodes.Status400BadRequest
                },

                Notifier = ex.Notifier
            };

        private static ElwarkProblemDetails Create(ElwarkConfirmationException ex) =>
            new ElwarkProblemDetails
            {
                Title = ex.Group,
                Type = ex.Type,
                Instance = ex.Source,
                Detail = GetDetails(GetKey(ex)),
                Status = ex.Code switch
                {
                    ConfirmationError.NotFound => StatusCodes.Status404NotFound,
                    _ => StatusCodes.Status400BadRequest
                }
            };

        private static ConfirmationProblemDetails Create(ElwarkConfirmationAlreadySentException ex) =>
            new ConfirmationProblemDetails
            {
                Title = ex.Group,
                Type = ex.Type,
                Instance = ex.Source,
                Detail = GetDetails(ex),
                Status = StatusCodes.Status400BadRequest,
                RetryAfter = ex.RetryAfter
            };

        private static ElwarkProblemDetails Create(ElwarkException ex,
            HttpStatusCode statusCode = HttpStatusCode.BadRequest) =>
            new ElwarkProblemDetails
            {
                Title = ex.Group,
                Type = ex.Type,
                Instance = ex.Source,
                Detail = GetDetails(GetKey(ex)),
                Status = (int) statusCode
            };

        private static ValidationProblemDetails Create(ElwarkModelStateException ex)
        {
            var result = ex.Failures.GroupBy(x => x.PropertyName)
                .Select(x => new KeyValuePair<string, IEnumerable<string>>(x.Key, x.Select(t => t.ErrorMessage)))
                .ToDictionary(x => x.Key, x => x.Value.ToArray());

            return new ValidationProblemDetails(result)
            {
                Title = nameof(CommonError),
                Type = nameof(CommonError.InvalidModelState),
                Status = StatusCodes.Status400BadRequest,
                Detail = GetDetails(ex)
            };
        }

        private static ValidationProblemDetails Create(string? name, string message) =>
            new ValidationProblemDetails(new Dictionary<string, string[]> {{name ?? "", new[] {message}}})
            {
                Title = nameof(CommonError),
                Type = nameof(CommonError.InvalidModelState),
                Status = StatusCodes.Status400BadRequest,
                Detail = GetDetails(GetKey(nameof(CommonError), CommonError.InvalidModelState.ToString()))
            };

        private static string GetKey(string group, string type) => $"{group}:{type}";

        private static string GetKey(ElwarkException ex) => GetKey(ex.Group, ex.Type);

        private static string GetDetails(string key, params object?[] values) =>
            string.Format(ErrorMessageResources.ResourceManager.GetString(key) ?? string.Empty, values);

        private static string GetDetails(ElwarkConfirmationAlreadySentException ex)
        {
            var retryAfter = ex.RetryAfter.ToString(DateTimeOffset.UtcNow - ex.RetryAfter >= TimeSpan.FromDays(1)
                ? "G"
                : "T"
            );

            return GetDetails(GetKey(ex), retryAfter);
        }

        private static string GetDetails(ElwarkModelStateException ex)
        {
            var sb = new StringBuilder();
            sb.AppendLine(GetDetails(GetKey(ex)));

            foreach (var item in ex.Failures.GroupBy(x => x.PropertyName))
                sb.AppendLine($"{item.Key.Capitalize()}: {string.Join(',', item.Select(x => x.ErrorMessage))}.");

            return sb.ToString();
        }
    }
}