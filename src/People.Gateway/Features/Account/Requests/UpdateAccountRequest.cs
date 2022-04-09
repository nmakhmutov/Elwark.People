using People.Grpc.Common;

namespace People.Gateway.Features.Account.Requests;

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
