using People.Grpc.Common;

namespace People.Gateway.Endpoints.Management.Request;

public sealed record UpdateAccountRequest(
    string Nickname,
    bool PreferNickname,
    string? FirstName,
    string? LastName,
    string Language,
    string Picture,
    string CountryCode,
    string TimeZone,
    DayOfWeek WeekStart,
    string DateFormat,
    string TimeFormat
);
