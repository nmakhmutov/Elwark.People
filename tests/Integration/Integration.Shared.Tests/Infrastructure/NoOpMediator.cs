using System.Runtime.CompilerServices;
using Mediator;

namespace Integration.Shared.Tests.Infrastructure;

/// <summary>Minimal <see cref="IMediator"/> for integration tests that must not publish or send.</summary>
public sealed class NoOpMediator : IMediator
{
    public async IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request,
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        await Task.Yield();
        yield break;
    }

    public async IAsyncEnumerable<object?> CreateStream(
        object request,
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        await Task.Yield();
        yield break;
    }

    public async IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamQuery<TResponse> query,
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        await Task.Yield();
        yield break;
    }

    public async IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamCommand<TResponse> command,
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        await Task.Yield();
        yield break;
    }

    public ValueTask Publish<TNotification>(TNotification notification, CancellationToken ct)
        where TNotification : INotification =>
        ValueTask.CompletedTask;

    public ValueTask Publish(object notification, CancellationToken ct) =>
        ValueTask.CompletedTask;

    public ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct) =>
        ValueTask.FromResult<TResponse>(default!);

    public ValueTask<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct) =>
        ValueTask.FromResult<TResponse>(default!);

    public ValueTask<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken ct) =>
        ValueTask.FromResult<TResponse>(default!);

    public ValueTask<object?> Send(object request, CancellationToken ct) =>
        ValueTask.FromResult<object?>(null);
}
