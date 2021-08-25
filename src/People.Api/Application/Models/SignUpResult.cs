using MongoDB.Bson;
using People.Domain.Aggregates.AccountAggregate;

namespace People.Api.Application.Models
{
    public sealed record SignUpResult(AccountId Id, string FullName, ObjectId? ConfirmationId);
}
