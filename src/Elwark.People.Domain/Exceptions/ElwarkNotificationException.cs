using Elwark.People.Abstractions;
using Elwark.People.Domain.ErrorCodes;

namespace Elwark.People.Domain.Exceptions
{
    public class ElwarkNotificationException : ElwarkException
    {
        public ElwarkNotificationException(NotificationError code, Notification? notifier = null)
            : base(nameof(NotificationError), code.ToString("G"))
        {
            Code = code;
            Notifier = notifier;
        }

        public Notification? Notifier { get; }

        public NotificationError Code { get; }
    }
}