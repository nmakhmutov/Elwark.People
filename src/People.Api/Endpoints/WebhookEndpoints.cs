using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using People.Api.Infrastructure;
using People.Api.Infrastructure.Filters;
using People.Application.Webhooks;

namespace People.Api.Endpoints;

internal static class WebhookEndpoints
{
    public static IEndpointRouteBuilder MapWebhookEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/webhooks")
            .WithTags("Webhooks")
            .RequireAuthorization(Policy.RequireAdmin.Name);

        group.MapGet("/", GetAllAsync);
        group.MapGet("/{id:guid}", GetByIdAsync);
        group.MapPost("/", CreateAsync)
            .AddEndpointFilter<ValidatorFilter<CreateRequest>>();
        group.MapPut("/{id:guid}", UpdateAsync)
            .AddEndpointFilter<ValidatorFilter<UpdateRequest>>();
        group.MapDelete("/{id:guid}", DeleteAsync);

        return routes;
    }

    private static IAsyncEnumerable<WebhookResponse> GetAllAsync(
        IWebhookConsumerRepository repository,
        CancellationToken ct
    ) =>
        repository.GetAsync(ct).Select(WebhookResponse.Map);

    private static async Task<Results<Ok<WebhookResponse>, NotFound>> GetByIdAsync(
        Guid id,
        IWebhookConsumerRepository repository,
        CancellationToken ct
    )
    {
        var consumer = await repository.GetAsync(id, ct);
        return consumer is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(WebhookResponse.Map(consumer));
    }

    private static async Task<WebhookResponse> CreateAsync(
        CreateRequest request,
        IWebhookConsumerRepository repository,
        CancellationToken ct
    )
    {
        var consumer = WebhookConsumer.Create(request.Type, request.Method, request.DestinationUrl, request.Token);
        var created = await repository.CreateAsync(consumer, ct);
        return WebhookResponse.Map(created);
    }

    private static async Task<Results<Ok<WebhookResponse>, NotFound>> UpdateAsync(
        Guid id,
        UpdateRequest request,
        IWebhookConsumerRepository repository,
        CancellationToken ct
    )
    {
        var consumer = await repository.GetAsync(id, ct);
        if (consumer is null)
            return TypedResults.NotFound();

        consumer.Update(request.Type, request.Method, request.DestinationUrl, request.Token);
        var updated = await repository.UpdateAsync(consumer, ct);

        return TypedResults.Ok(WebhookResponse.Map(updated));
    }

    private static async Task<Results<NoContent, NotFound>> DeleteAsync(
        Guid id,
        IWebhookConsumerRepository repository,
        CancellationToken ct
    )
    {
        var consumer = await repository.GetAsync(id, ct);
        if (consumer is null)
            return TypedResults.NotFound();

        await repository.DeleteAsync(id, ct);
        return TypedResults.NoContent();
    }

    internal sealed record WebhookResponse(Guid Id, string Type, string Method, string DestinationUrl, string? Token)
    {
        internal static WebhookResponse Map(WebhookConsumer consumer) =>
            new(
                consumer.Id,
                consumer.Type.ToString(),
                consumer.Method.ToString(),
                consumer.DestinationUrl,
                consumer.Token);
    }

    internal sealed record CreateRequest(WebhookType Type, WebhookMethod Method, string DestinationUrl, string? Token)
    {
        internal sealed class Validator : AbstractValidator<CreateRequest>
        {
            public Validator()
            {
                RuleFor(x => x.Type).IsInEnum();
                RuleFor(x => x.Method).IsInEnum();
                RuleFor(x => x.DestinationUrl)
                    .NotEmpty()
                    .MaximumLength(2048)
                    .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
                    .WithMessage("Must be a valid absolute URL.");
                RuleFor(x => x.Token).MaximumLength(256);
            }
        }
    }

    internal sealed record UpdateRequest(WebhookType Type, WebhookMethod Method, string DestinationUrl, string? Token)
    {
        internal sealed class Validator : AbstractValidator<UpdateRequest>
        {
            public Validator()
            {
                RuleFor(x => x.Type).IsInEnum();

                RuleFor(x => x.Method).IsInEnum();

                RuleFor(x => x.DestinationUrl)
                    .NotEmpty()
                    .MaximumLength(2048)
                    .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
                    .WithMessage("Must be a valid absolute URL.");

                RuleFor(x => x.Token).MaximumLength(256);
            }
        }
    }
}
