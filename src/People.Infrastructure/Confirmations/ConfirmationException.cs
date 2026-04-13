namespace People.Infrastructure.Confirmations;

public sealed class ConfirmationException : Exception
{
    public string Code { get; }

    public ConfirmationException(string code, string? message = null)
        : base(message) =>
        Code = code;

    public static ConfirmationException NotFound() =>
        new("NotFound");

    public static ConfirmationException Mismatch() =>
        new("Mismatch");

    public static ConfirmationException AlreadySent() =>
        new("AlreadySent");
}
