namespace Gateway.Api.Requests;

public sealed record CreatePassword(string Id, uint Code, string Password);
