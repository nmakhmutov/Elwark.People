using System.Net.Mail;

namespace People.Application.Providers.Gravatar;

public interface IGravatarService
{
    Task<GravatarProfile?> GetAsync(MailAddress email);
}
