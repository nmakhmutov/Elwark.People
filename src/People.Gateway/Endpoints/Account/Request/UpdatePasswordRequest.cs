namespace People.Gateway.Endpoints.Account.Request;

public sealed record UpdatePasswordRequest(string OldPassword, string NewPassword);
