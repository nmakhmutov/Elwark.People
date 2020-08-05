using Elwark.People.Abstractions;

namespace Elwark.People.Api.Application.ProblemDetails
{
    public class NotificationProblemDetails : Microsoft.AspNetCore.Mvc.ProblemDetails
    {
        public Notification? Notifier { get; set; }
    }
}