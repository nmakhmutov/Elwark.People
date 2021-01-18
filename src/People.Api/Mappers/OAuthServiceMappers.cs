using System;
using Google.Protobuf.WellKnownTypes;
using People.Api.Application.Models;
using People.Domain.AggregateModels.Account;

namespace People.Api.Mappers
{
    public static class OAuthServiceMappers
    {
        public static People.Grpc.Identity.AccountReply ToAccountReply(this Account account) =>
            new()
            {
                Id = account.Id.ToGrpcAccountId(),
                Address = new People.Grpc.Identity.AccountReply.Types.Address
                {
                    CityName = account.Address.City,
                    CountryCode = account.Address.CountryCode.IsEmpty()
                        ? string.Empty
                        : account.Address.CountryCode.ToString()
                },
                Ban = account.Ban.ToGrpcBan(),
                Name = new People.Grpc.Identity.AccountReply.Types.Name
                {
                    Nickname = account.Name.Nickname,
                    FirstName = account.Name.FirstName,
                    LastName = account.Name.LastName,
                    FullName = account.Name.FullName()
                },
                Email = account.GetPrimaryEmail().ToGrpcPrimaryEmail(),
                Profile = new People.Grpc.Identity.AccountReply.Types.Profile
                {
                    Bio = account.Profile.Bio,
                    Birthday = account.Profile.Birthday?.ToTimestamp(),
                    Gender = account.Profile.Gender.ToGrpcGender(),
                    Language = account.Profile.Language.ToString(),
                    Picture = account.Profile.Picture.ToString()
                },
                Timezone = new People.Grpc.Identity.AccountReply.Types.Timezone
                {
                    Name = account.Timezone.Name,
                    Offset = account.Timezone.Offset.ToDuration()
                },
                UpdatedAt = account.UpdatedAt.ToTimestamp()
            };

        public static People.Grpc.Identity.SignUpReply ToSignUpReply(this SignUpResult result) =>
            new()
            {
                Id = result.Id.ToGrpcAccountId(),
                DisplayName = result.FullName,
                IsSentConfirmation = result.IsSentConfirmation
            };
        
        
        public static People.Grpc.Identity.SignInReply ToSignInReply(this SignInResult result) =>
            new()
            {
                Id = result.Id.ToGrpcAccountId(),
                DisplayName = result.FullName
            };

        private static People.Grpc.Identity.AccountReply.Types.PrimaryEmail ToGrpcPrimaryEmail(this AccountEmail email) =>
            new()
            {
                Email = email.Address,
                IsConfirmed = email.IsConfirmed
            };

        private static People.Grpc.Identity.AccountReply.Types.Ban? ToGrpcBan(this Ban? ban) =>
            ban switch
            {
                PermanentBan x => new People.Grpc.Identity.AccountReply.Types.Ban
                {
                    Reason = x.Reason,
                    ExpiresAt = null
                },

                TemporaryBan x => new People.Grpc.Identity.AccountReply.Types.Ban
                {
                    Reason = x.Reason,
                    ExpiresAt = x.ExpiredAt.ToTimestamp()
                },

                null => null,

                _ => throw new ArgumentOutOfRangeException(nameof(ban), ban, "Unknown ban type")
            };
    }
}