namespace People.Gateway.Requests;

public sealed record UpdatePassword(string OldPassword, string NewPassword);
