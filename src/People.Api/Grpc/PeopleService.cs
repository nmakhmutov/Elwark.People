using System.Net;
using System.Net.Mail;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using People.Api.Application.Commands.AppendGoogle;
using People.Api.Application.Commands.AppendMicrosoft;
using People.Api.Application.Commands.SignInByEmail;
using People.Api.Application.Commands.SignInByGoogle;
using People.Api.Application.Commands.SignInByMicrosoft;
using People.Api.Application.Commands.SigningInByEmail;
using People.Api.Application.Commands.SigningUpByEmail;
using People.Api.Application.Commands.SignUpByEmail;
using People.Api.Application.Commands.SignUpByGoogle;
using People.Api.Application.Commands.SignUpByMicrosoft;
using People.Api.Application.Queries.GetAccountSummary;
using People.Api.Application.Queries.IsAccountActive;
using People.Domain.AggregatesModel.AccountAggregate;
using People.Grpc.People;

namespace People.Api.Grpc;

internal sealed class PeopleService : People.Grpc.People.PeopleService.PeopleServiceBase
{
    private readonly IMediator _mediator;

    public PeopleService(IMediator mediator) =>
        _mediator = mediator;

    public override async Task<AccountReply> GetAccount(AccountRequest request, ServerCallContext context)
    {
        var result = await _mediator.Send(new GetAccountSummaryQuery(request.Id), context.CancellationToken);
        return result.ToGrpc();
    }

    public override async Task<BoolValue> IsAccountActive(AccountRequest request, ServerCallContext context)
    {
        var result = await _mediator.Send(new IsAccountActiveQuery(request.Id), context.CancellationToken);
        return new BoolValue { Value = result };
    }

    public override async Task<EmailSigningUpReply> SigningUpByEmail(EmailSigningUpRequest request,
        ServerCallContext context)
    {
        var command = new SigningUpByEmailCommand(
            new MailAddress(request.Email),
            Language.Parse(request.Language),
            ParseIpAddress(request.Ip)
        );

        return new EmailSigningUpReply { Token = await _mediator.Send(command, context.CancellationToken) };
    }

    public override async Task<SignUpReply> SignUpByEmail(EmailSignUpRequest request, ServerCallContext context)
    {
        var command = new SignUpByEmailCommand(request.Token, request.Code);
        var result = await _mediator.Send(command, context.CancellationToken);

        return result.ToGrpc();
    }

    public override async Task<EmailSigningInReply> SigningInByEmail(EmailSigningInRequest request,
        ServerCallContext context)
    {
        var command = new SigningInByEmailCommand(new MailAddress(request.Email), Language.Parse(request.Language));

        return new EmailSigningInReply
        {
            Token = await _mediator.Send(command, context.CancellationToken)
        };
    }

    public override async Task<SignInReply> SignInByEmail(EmailSignInRequest request, ServerCallContext context)
    {
        var command = new SignInByEmailCommand(request.Token, request.Code);
        var result = await _mediator.Send(command, context.CancellationToken);

        return result.ToGrpc();
    }

    public override async Task<SignUpReply> SignUpByGoogle(ExternalSignUpRequest request, ServerCallContext context)
    {
        var command = new SignUpByGoogleCommand(
            request.AccessToken,
            Language.Parse(request.Language),
            ParseIpAddress(request.Ip)
        );
        var result = await _mediator.Send(command, context.CancellationToken);

        return result.ToGrpc();
    }

    public override async Task<SignInReply> SignInByGoogle(ExternalSignInRequest request, ServerCallContext context)
    {
        var command = new SignInByGoogleCommand(request.AccessToken, ParseIpAddress(request.Ip));
        var result = await _mediator.Send(command, context.CancellationToken);

        return result.ToGrpc();
    }

    public override async Task<Empty> AppendGoogle(ExternalAppendRequest request, ServerCallContext context)
    {
        var command = new AppendGoogleCommand(request.Id, request.AccessToken);
        await _mediator.Send(command, context.CancellationToken);

        return new Empty();
    }

    public override async Task<SignUpReply> SignUpByMicrosoft(ExternalSignUpRequest request, ServerCallContext context)
    {
        var command = new SignUpByMicrosoftCommand(
            request.AccessToken,
            Language.Parse(request.Language),
            ParseIpAddress(request.Ip)
        );
        var result = await _mediator.Send(command, context.CancellationToken);

        return result.ToGrpc();
    }

    public override async Task<SignInReply> SignInByMicrosoft(ExternalSignInRequest request, ServerCallContext context)
    {
        var command = new SignInByMicrosoftCommand(request.AccessToken, ParseIpAddress(request.Ip));
        var result = await _mediator.Send(command, context.CancellationToken);

        return result.ToGrpc();
    }

    public override async Task<Empty> AppendMicrosoft(ExternalAppendRequest request, ServerCallContext context)
    {
        var command = new AppendMicrosoftCommand(request.Id, request.AccessToken);
        await _mediator.Send(command, context.CancellationToken);

        return new Empty();
    }

    private static IPAddress ParseIpAddress(string? ip) =>
        IPAddress.TryParse(ip, out var value) ? value : IPAddress.None;
}
