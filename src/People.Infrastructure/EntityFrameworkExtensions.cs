using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using People.Domain.AggregatesModel.AccountAggregate;

namespace People.Infrastructure;

public static class EntityFrameworkExtensions
{
    public static Task<bool> IsEmailExistsAsync(
        this IQueryable<EmailAccount> emails,
        MailAddress email,
        CancellationToken ct = default)
    {
        if (email is null)
            throw new ArgumentNullException(nameof(email));
        
        return emails.AnyAsync(x => x.Email == email.Address, ct);
    }

    public static Task<bool> IsGoogleExistsAsync(
        this IQueryable<ExternalConnection> accounts,
        string identity,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(identity))
            throw new ArgumentException("Value cannot be null or empty.", nameof(identity));

        return accounts.AnyAsync(x => x.Type == ExternalService.Google && x.Identity == identity, ct);
    }

    public static Task<bool> IsMicrosoftExistsAsync(
        this IQueryable<ExternalConnection> accounts,
        string identity,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(identity))
            throw new ArgumentException("Value cannot be null or empty.", nameof(identity));

        return accounts.AnyAsync(x => x.Type == ExternalService.Microsoft && x.Identity == identity, ct);
    }
}
