using System.Threading.Tasks;

namespace People.Notification.Services
{
    public interface IEmailProvider
    {
        Task SendEmailAsync(string email, string subject, string body);
    }
}