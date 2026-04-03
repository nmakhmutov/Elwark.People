namespace Unit.Api.Tests.Infrastructure.ExternalServices;

/// <summary>
/// Configurable <see cref="HttpMessageHandler"/> for unit tests (no real network I/O).
/// </summary>
internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler =
        (_, _) => throw new InvalidOperationException("Configure MockHttpMessageHandler before sending a request.");

    public int SendCallCount { get; private set; }

    /// <summary>Replace the handler used for all subsequent requests.</summary>
    public void Configure(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) =>
        _handler = handler;

    /// <summary>Return a fixed response for every request.</summary>
    public void ConfigureResponse(HttpResponseMessage response) =>
        Configure((_, _) => Task.FromResult(response));

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        SendCallCount++;
        return _handler(request, cancellationToken);
    }
}
