using System.Threading;
using System.Threading.Tasks;
using People.Domain.AggregateModels.Account;

namespace People.Infrastructure.Confirmations
{
    public interface IConfirmationService
    {
        Task<int> CreateSignUpConfirmation(AccountId id, CancellationToken ct = default);
        Task<Confirmation?> GetSignUpConfirmation(AccountId id, CancellationToken ct = default);
        
        Task<int> CreateResetPasswordConfirmation(AccountId id, CancellationToken ct = default);
        Task<Confirmation?> GetResetPasswordConfirmation(AccountId id, CancellationToken ct = default);
    }
}