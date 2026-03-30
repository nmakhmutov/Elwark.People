using System.Net;
using People.Api.Infrastructure.Providers.Google;
using Xunit;

namespace People.UnitTests.Infrastructure.ExternalServices;

public sealed class GoogleApiServiceTests
{
    private const string Token = "ya29.test-access-token";

    private static GoogleApiService CreateService(MockHttpMessageHandler handler, Uri? baseAddress = null)
    {
        var client = new HttpClient(handler, disposeHandler: true)
        {
            BaseAddress = baseAddress ?? new Uri("https://google.test/")
        };
        return new GoogleApiService(client);
    }

    [Fact]
    public async Task GetAsync_ValidJson_ReturnsGoogleAccount()
    {
        var handler = new MockHttpMessageHandler();
        handler.Configure((req, _) =>
        {
            Assert.Equal("/oauth2/v1/userinfo", req.RequestUri?.PathAndQuery);
            var json = """
                       {"id":"gid-1","email":"user@gmail.com","verified_email":true,
                        "given_name":"Ann","family_name":"Lee","locale":"en-US",
                        "picture":"https://cdn.example/p.png"}
                       """;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });
        });

        var sut = CreateService(handler);
        var account = await sut.GetAsync(Token, CancellationToken.None);

        Assert.Equal("gid-1", account.Identity);
        Assert.Equal("user@gmail.com", account.Email.Address);
        Assert.True(account.IsEmailVerified);
        Assert.Equal("Ann", account.FirstName);
        Assert.Equal("Lee", account.LastName);
        Assert.NotNull(account.Picture);
        Assert.Equal("en-US", account.Locale?.Name);
    }

    [Fact]
    public async Task GetAsync_SendsBearerTokenInAuthorizationHeader()
    {
        HttpRequestMessage? captured = null;
        var handler = new MockHttpMessageHandler();
        handler.Configure((req, _) =>
        {
            captured = req;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"id":"x","email":"a@b.com","verified_email":false}""",
                    System.Text.Encoding.UTF8, "application/json")
            });
        });

        var sut = CreateService(handler);
        await sut.GetAsync(Token, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.NotNull(captured.Headers.Authorization);
        Assert.Equal("Bearer", captured.Headers.Authorization.Scheme);
        Assert.Equal(Token, captured.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task GetAsync_Unauthorized_ThrowsHttpRequestException()
    {
        var handler = new MockHttpMessageHandler();
        handler.ConfigureResponse(new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var sut = CreateService(handler);

        await Assert.ThrowsAsync<HttpRequestException>(() => sut.GetAsync(Token, CancellationToken.None));
    }

    [Fact]
    public async Task GetAsync_ServerError_ThrowsHttpRequestException()
    {
        var handler = new MockHttpMessageHandler();
        handler.ConfigureResponse(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var sut = CreateService(handler);

        await Assert.ThrowsAsync<HttpRequestException>(() => sut.GetAsync(Token, CancellationToken.None));
    }

    [Fact]
    public async Task GetAsync_MalformedJson_ThrowsJsonException()
    {
        var handler = new MockHttpMessageHandler();
        handler.ConfigureResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{not-json", System.Text.Encoding.UTF8, "application/json")
        });

        var sut = CreateService(handler);

        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() => sut.GetAsync(Token, CancellationToken.None));
    }

    [Fact]
    public async Task GetAsync_NetworkTimeout_ThrowsTaskCanceledException()
    {
        var handler = new MockHttpMessageHandler();
        handler.Configure((_, ct) => Task.FromException<HttpResponseMessage>(
            new TaskCanceledException("Simulated timeout", null, ct)));

        var sut = CreateService(handler);

        await Assert.ThrowsAsync<TaskCanceledException>(() => sut.GetAsync(Token, CancellationToken.None));
    }
}
