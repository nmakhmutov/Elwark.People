using System;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using Elwark.People.Domain.Exceptions;
using MediatR;

namespace Elwark.People.Api.Application.Commands
{
    public class UpdatePasswordCommand : IRequest
    {
        public UpdatePasswordCommand(AccountId accountId, string? oldPassword, string newPassword)
        {
            AccountId = accountId;
            OldPassword = oldPassword;
            NewPassword = newPassword;
        }

        public AccountId AccountId { get; }

        public string? OldPassword { get; }

        public string NewPassword { get; }
    }

    public class UpdatePasswordCommandHandler : IRequestHandler<UpdatePasswordCommand>
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IPasswordValidator _passwordValidator;

        public UpdatePasswordCommandHandler(IAccountRepository accountRepository, IPasswordHasher passwordHasher,
            IPasswordValidator passwordValidator)
        {
            _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
            _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
            _passwordValidator = passwordValidator;
        }

        public async Task<Unit> Handle(UpdatePasswordCommand request, CancellationToken cancellationToken)
        {
            var account = await _accountRepository.GetAsync(request.AccountId, cancellationToken)
                          ?? throw ElwarkAccountException.NotFound(request.AccountId);

            if (account.Password is {}) 
                account.CheckPassword(request.OldPassword, _passwordHasher);

            await account.SetPasswordAsync(request.NewPassword, _passwordValidator, _passwordHasher);

            _accountRepository.Update(account);
            await _accountRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}