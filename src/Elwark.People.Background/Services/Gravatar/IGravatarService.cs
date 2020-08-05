using System.Threading.Tasks;
using Elwark.People.Abstractions;

namespace Elwark.People.Background.Services.Gravatar
{
    public interface IGravatarService
    {
        Task<GravatarProfile?> GetAsync(Notification.PrimaryEmail email);
    }
}