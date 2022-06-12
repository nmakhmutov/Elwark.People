using System.Net.Mail;

namespace People.Api.Infrastructure.Providers.Gravatar;

public interface IGravatarService
{
    Task<GravatarProfile?> GetAsync(MailAddress email);
}
