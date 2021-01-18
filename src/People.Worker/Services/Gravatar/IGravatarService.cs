using System.Threading.Tasks;

namespace People.Worker.Services.Gravatar
{
    public interface IGravatarService
    {
        Task<GravatarProfile?> GetAsync(string email);
    }
}