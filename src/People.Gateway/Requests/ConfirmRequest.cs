namespace People.Gateway.Requests;

public sealed record ConfirmRequest(string Id, uint Code);
