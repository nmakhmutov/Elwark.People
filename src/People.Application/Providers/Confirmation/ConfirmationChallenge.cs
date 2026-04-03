namespace People.Application.Providers.Confirmation;

public sealed record ConfirmationChallenge(Guid Id, string Token, string Code);
