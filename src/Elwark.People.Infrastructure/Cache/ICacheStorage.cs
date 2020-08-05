using System;
using System.Threading.Tasks;

namespace Elwark.People.Infrastructure.Cache
{
    public interface ICacheStorage
    {
        Task<bool> CreateAsync<T>(string key, T data, TimeSpan expiry);

        Task<T> ReadAsync<T>(string key);

        Task<bool> DeleteAsync(string key);
    }
}