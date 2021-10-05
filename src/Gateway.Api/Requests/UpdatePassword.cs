namespace Gateway.Api.Requests;

public sealed record UpdatePassword(string OldPassword, string NewPassword);
