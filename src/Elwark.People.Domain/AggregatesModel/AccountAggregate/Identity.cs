using System;
using Elwark.People.Abstractions;
using Elwark.People.Domain.SeedWork;

namespace Elwark.People.Domain.AggregatesModel.AccountAggregate
{
    public class Identity : Entity<Guid>
    {
        private readonly long _accountId;
        private readonly DateTimeOffset _createdAt;

        protected Identity()
        {
            _accountId = default;
            IdentificationType = IdentificationType.Email;
            NotificationType = NotificationType.PrimaryEmail;
            Value = "none@elwark.com";
            _createdAt = DateTimeOffset.UtcNow;
        }

        public Identity(IdentificationType identificationType, NotificationType notificationType, string value)
        {
            IdentificationType = identificationType;
            NotificationType = notificationType;
            Value = value;
        }

        public AccountId AccountId => new AccountId(_accountId);

        public IdentificationType IdentificationType { get; }

        public NotificationType NotificationType { get; private set; }

        public string Value { get; }

        public DateTimeOffset? ConfirmedAt { get; private set; }

        public DateTime CreatedAt =>
            _createdAt.UtcDateTime;

        public Identification Identification =>
            Identification.Create(IdentificationType, Value);

        public Notification Notification =>
            Notification.Create(NotificationType, Value);

        public bool IsConfirmed =>
            ConfirmedAt.HasValue;

        public void Confirm() =>
            ConfirmedAt = DateTimeOffset.UtcNow;

        public void SetNotificationType(NotificationType type) =>
            NotificationType = type;
    }
}