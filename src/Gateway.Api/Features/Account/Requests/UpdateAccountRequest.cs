using People.Grpc.Common;

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