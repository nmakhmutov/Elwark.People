using System;
using System.Threading;
using System.Threading.Tasks;

namespace Elwark.People.Shared
{
    public interface IRetryPolicy
    {
        Task<TResult> ExecuteAsync<TResult>(Func<CancellationToken, Task<TResult>> action,
            CancellationToken cancellationToken);
    }
}