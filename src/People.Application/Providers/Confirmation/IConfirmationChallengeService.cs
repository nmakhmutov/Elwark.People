using People.Domain.Entities;

namespace People.Application.Providers.Confirmation;

public interface IConfirmationChallengeService
{
    Task<ConfirmationChallenge> IssueAsync(AccountId id, ConfirmationType type, CancellationToken ct = default);

    Task<AccountId> VerifyAsync(string token, string code, ConfirmationType type, CancellationToken ct = default);

    Task<int> DeleteByAccountAsync(AccountId id, CancellationToken ct = default);
}
