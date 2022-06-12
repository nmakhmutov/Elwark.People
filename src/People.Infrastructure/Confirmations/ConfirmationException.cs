namespace People.Infrastructure.Confirmations;

public sealed class ConfirmationException : Exception
{
    public ConfirmationException(string code, string? message = null)
        : base(message) =>
        Code = code;

    public string Code { get; }

    public static ConfirmationException NotFound() =>
        new("NotFound");

    public static ConfirmationException Mismatch() =>
        new("Mismatch");

    public static ConfirmationException AlreadySent() =>
        new("AlreadySent");
}
