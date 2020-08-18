using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Domain.ErrorCodes;
using Elwark.People.Domain.Events;
using Elwark.People.Domain.Exceptions;
using Elwark.People.Domain.SeedWork;

namespace Elwark.People.Domain.AggregatesModel.AccountAggregate
{
    public class Account : Entity<long>, IAggregateRoot
    {
        private readonly DateTimeOffset _createdAt;
        private readonly List<Identity> _identities = new List<Identity>();
        private readonly List<string> _roles = new List<string>();

        private DateTimeOffset _updatedAt;
        private Uri _picture;

        protected Account()
        {
            _createdAt = _updatedAt = DateTimeOffset.UtcNow;
            _picture = new Uri("about:blank");
            Name = new Name(string.Empty);
            Address = new Address(null, null);
            Links = new Links();
            BasicInfo = new BasicInfo(CultureInfo.InvariantCulture);
        }

        public Account(Name name, CultureInfo language, Uri picture)
        {
            _createdAt = _updatedAt = DateTimeOffset.UtcNow;
            _picture = picture;

            Name = name;
            BasicInfo = new BasicInfo(language);
            Address = new Address(null, null);
            Links = new Links();

            AddDomainEvent(new AccountCreatedDomainEvent(this));
        }

        public Account(Name name, BasicInfo info, Uri picture, Links links)
        {
            _createdAt = _updatedAt = DateTimeOffset.UtcNow;
            _picture = picture;

            Name = name;
            Address = new Address(null, null);
            Links = links;
            BasicInfo = info;

            AddDomainEvent(new AccountCreatedDomainEvent(this));
        }

        public Password? Password { get; private set; }

        public Ban? Ban { get; private set; }

        public Name Name { get; private set; }

        public Address Address { get; private set; }

        public Links Links { get; private set; }

        public BasicInfo BasicInfo { get; private set; }

        public Uri Picture => _picture;

        public DateTime CreatedAt => _createdAt.UtcDateTime;

        public DateTime UpdatedAt => _updatedAt.UtcDateTime;

        public IReadOnlyCollection<Identity> Identities => _identities.AsReadOnly();

        public IReadOnlyCollection<string> Roles => _roles.AsReadOnly();

        public bool IsBanned()
        {
            if (Ban is null)
                return false;

            return Ban.ExpiredAt > DateTimeOffset.UtcNow;
        }

        public void SetBan(Ban ban)
        {
            Ban = ban ?? throw new ArgumentNullException(nameof(ban));

            AddDomainEvent(new BanAddedDomainEvent(this, ban));
            AccountUpdated();
        }

        public void RemoveBan()
        {
            Ban = null;

            AddDomainEvent(new BanRemovedDomainEvent(this));
            AccountUpdated();
        }

        public Task AddIdentificationAsync(Identification.Email email, IIdentificationValidator validator) =>
            AddIdentificationAsync(email, false, validator);

        public async Task AddIdentificationAsync(Identification identification, bool isConfirmed,
            IIdentificationValidator validator)
        {
            if (identification is null)
                throw new ArgumentNullException(nameof(identification));

            if (validator is null)
                throw new ArgumentNullException(nameof(validator));

            if (_identities.Any(x => x.Identification == identification))
                return;

            await validator.CheckUniquenessAsync(identification);

            NotificationType GetNotification()
            {
                if (!(identification is Identification.Email))
                    return NotificationType.None;

                if (_identities.Count == 0)
                    return NotificationType.PrimaryEmail;

                if (GetSecondaryEmail() is null)
                    return NotificationType.SecondaryEmail;

                return NotificationType.None;
            }

            var accountIdentity = new Identity(identification.Type, GetNotification(), identification.Value);

            if (isConfirmed)
                accountIdentity.Confirm();

            _identities.Add(accountIdentity);

            AddDomainEvent(new IdentityAddedDomainEvent(this, identification));
            AccountUpdated();
        }

        public void ConfirmIdentity(IdentityId id)
        {
            var result = _identities.First(x => x.Id == id.Value);

            if (result.IsConfirmed)
                throw new ElwarkIdentificationException(IdentificationError.AlreadyConfirmed, result.Identification);

            result.Confirm();

            AddDomainEvent(new IdentityConfirmedDomainEvent(this, result.Identification));
            AccountUpdated();
        }

        public void RemoveIdentity(IdentityId id)
        {
            var item = _identities.FirstOrDefault(x => x.Id == id);
            if (item is null)
                return;

            if (item.Notification is Notification.PrimaryEmail)
                throw new ElwarkIdentificationException(IdentificationError.PrimaryEmail, item.Identification);

            if (_identities.Count == 1)
                throw new ElwarkIdentificationException(IdentificationError.LastIdentity, item.Identification);

            _identities.Remove(item);

            AddDomainEvent(new IdentityRemovedDomainEvent(this, item.Identification));
            AccountUpdated();
        }

        public Notification.PrimaryEmail GetPrimaryEmail()
        {
            var data = _identities.First(x => x.NotificationType == NotificationType.PrimaryEmail);
            return new Notification.PrimaryEmail(data.Value);
        }

        public Notification.SecondaryEmail? GetSecondaryEmail()
        {
            var data = _identities.FirstOrDefault(x => x.NotificationType == NotificationType.SecondaryEmail);

            return data is null
                ? null
                : new Notification.SecondaryEmail(data.Value);
        }

        public void SetNotificationType(IdentityId identityId, NotificationType type)
        {
            var identity = _identities.FirstOrDefault(x => x.Id == identityId)
                           ?? throw ElwarkIdentificationException.NotFound();

            if (!identity.IsConfirmed)
                throw new ElwarkIdentificationException(IdentificationError.NotConfirmed, identity.Identification);

            switch (IdentifierType: identity.IdentificationType, identity.NotificationType)
            {
                case (IdentificationType.Email, NotificationType.None):
                case (IdentificationType.Email, NotificationType.SecondaryEmail):
                    _identities.FirstOrDefault(x => x.NotificationType == type)
                        ?.SetNotificationType(NotificationType.None);
                    identity.SetNotificationType(type);
                    break;

                default:
                    throw new ElwarkNotificationException(NotificationError.UnsupportedTransformation);
            }

            AccountUpdated();
        }

        public void AddRole(string name)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));

            if (_roles.Contains(name))
                return;

            _roles.Add(name);

            AddDomainEvent(new RoleAddedDomainEvent(this, name));
            AccountUpdated();
        }

        public void RemoveRole(string name)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));

            if (!_roles.Contains(name))
                return;

            _roles.Remove(name);

            AddDomainEvent(new RoleRemovedDomainEvent(this, name));
            AccountUpdated();
        }

        public async Task SetPasswordAsync(string password, IPasswordValidator validator, IPasswordHasher hasher)
        {
            if (password is null)
                throw new ArgumentNullException(nameof(password));

            if (hasher is null)
                throw new ArgumentNullException(nameof(hasher));

            await validator.ValidateAsync(password);

            var salt = hasher.CreateSalt();
            var passwordHash = hasher.CreatePasswordHash(password, salt);

            Password = new Password(passwordHash, salt);

            AddDomainEvent(new PasswordChangedDomainEvent(this));
            AccountUpdated();
        }

        public void CheckPassword(string? password, IPasswordHasher hasher)
        {
            if (password is null)
                throw new ElwarkPasswordException(PasswordError.Empty);

            if (hasher is null)
                throw new ArgumentNullException(nameof(hasher));

            if (Password is null)
                throw new ElwarkPasswordException(PasswordError.NotSet);

            if (!hasher.IsEqual(password, Password.Hash, Password.Salt))
                throw new ElwarkPasswordException(PasswordError.Mismatch);
        }

        public void SetName(Name name)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));

            if (Name.Equals(name))
                return;

            Name = name;

            AddDomainEvent(new NameChangedDomainEvent(this, name));
            AccountUpdated();
        }

        public void SetBasicInfo(BasicInfo info)
        {
            if (info is null)
                throw new ArgumentNullException(nameof(info));

            if (BasicInfo.Equals(info))
                return;

            BasicInfo = info;

            AddDomainEvent(new BasicInfoChangedDomainEvent(this, info));
            AccountUpdated();
        }

        public void SetAddress(Address address)
        {
            if (address is null)
                throw new ArgumentNullException(nameof(address));

            if (Address.Equals(address))
                return;

            Address = address;

            AddDomainEvent(new AddressChangedDomainEvent(this, address));
            AccountUpdated();
        }

        public void SetLinks(Links links)
        {
            if (links is null)
                throw new ArgumentNullException(nameof(links));

            if (Links.Equals(links))
                return;

            Links = links;

            AddDomainEvent(new LinksChangedDomainEvent(this, links));
            AccountUpdated();
        }

        public void SetPicture(Uri picture)
        {
            _picture = picture ?? throw new ArgumentNullException(nameof(picture));

            AccountUpdated();
        }

        private void AccountUpdated() =>
            _updatedAt = DateTimeOffset.UtcNow;
    }
}