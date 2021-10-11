using DayOfWeek = People.Grpc.Common.DayOfWeek;

namespace Gateway.Api.Features.Account.Requests;

public sealed record UpdateAccountRequest(
    string Nickname,
    bool PreferNickname,
    string? FirstName,
    string? LastName,
    string Language,
    string Picture,
    string CountryCode,
    string TimeZone,
    DayOfWeek FirstDayOfWeek
);
