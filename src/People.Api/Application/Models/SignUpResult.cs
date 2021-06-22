using MongoDB.Bson;
using People.Domain.Aggregates.Account;

namespace People.Api.Application.Models
{
    public sealed record SignUpResult(AccountId Id, string FullName, ObjectId? ConfirmationId);
}
