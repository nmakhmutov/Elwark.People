using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using People.Domain.Entities;

namespace People.Infrastructure;

public static class EntityFrameworkExtensions
{
    public static Task<bool> IsEmailExistsAsync(
        this IQueryable<EmailAccount> emails,
        MailAddress email,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(email);

        return emails.AnyAsync(x => x.Email == email.Address, ct);
    }

    public static Task<bool> IsGoogleExistsAsync(
        this IQueryable<ExternalConnection> accounts,
        string identity,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(identity);

        return accounts.AnyAsync(x => x.Type == ExternalService.Google && x.Identity == identity, ct);
    }

    public static Task<bool> IsMicrosoftExistsAsync(
        this IQueryable<ExternalConnection> accounts,
        string identity,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(identity);

        return accounts.AnyAsync(x => x.Type == ExternalService.Microsoft && x.Identity == identity, ct);
    }

    public static IQueryable<Account> WhereGoogle(this IQueryable<Account> source, string identity) =>
        source.Where(x => x.Externals.Any(e => e.Type == ExternalService.Google && e.Identity == identity));
    
    public static IQueryable<Account> WhereMicrosoft(this IQueryable<Account> source, string identity) =>
        source.Where(x => x.Externals.Any(e => e.Type == ExternalService.Microsoft && e.Identity == identity));
}
