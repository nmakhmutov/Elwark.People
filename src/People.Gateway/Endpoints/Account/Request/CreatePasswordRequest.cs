namespace People.Gateway.Endpoints.Account.Request;

public sealed record CreatePasswordRequest(string Token, uint Code, string Password);
