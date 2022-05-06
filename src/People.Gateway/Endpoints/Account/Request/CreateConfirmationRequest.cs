using FluentValidation;
using People.Grpc.Common;

namespace People.Gateway.Endpoints.Account.Request;

public sealed record CreateConfirmationRequest(IdentityType Type, string Value)
{
    public class Validator : AbstractValidator<CreateConfirmationRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Type)
                .IsInEnum();

            RuleFor(x => x.Value)
                .NotEmpty();
        }
    }
}
