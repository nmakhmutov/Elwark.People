using FluentValidation;
using People.Grpc.Common;

namespace People.Gateway.Endpoints.Account.Request;

public sealed record ConfirmConnectionRequest(
    IdentityType Type,
    string Value,
    string ConfirmationToken,
    uint ConfirmationCode
)
{
    public sealed class Validator : AbstractValidator<ConfirmConnectionRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Type)
                .IsInEnum();

            RuleFor(x => x.Value)
                .NotEmpty();

            RuleFor(x => x.ConfirmationToken)
                .NotEmpty();

            RuleFor(x => x.ConfirmationCode)
                .NotEmpty();
        }
    }
}
