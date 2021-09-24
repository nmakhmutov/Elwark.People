using System.Threading.Tasks;

namespace People.Account.Worker.Services.Gravatar
{
    public interface IGravatarService
    {
        Task<GravatarProfile?> GetAsync(string email);
    }
}