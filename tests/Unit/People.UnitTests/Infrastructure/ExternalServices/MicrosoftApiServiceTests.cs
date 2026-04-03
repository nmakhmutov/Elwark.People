using System.Net;
using People.Infrastructure.Providers;
using Xunit;

namespace People.UnitTests.Infrastructure.ExternalServices;

public sealed class MicrosoftApiServiceTests
{
    private const string Token = "ms-access-token";

    private static MicrosoftApiService CreateService(MockHttpMessageHandler handler) =>
        new(new HttpClient(handler, disposeHandler: true) { BaseAddress = new Uri("https://graph.test/") });

    [Fact]
    public async Task GetAsync_ValidJson_ReturnsMicrosoftAccount()
    {
        var handler = new MockHttpMessageHandler();
        handler.ConfigureResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"id":"ms-id","userPrincipalName":"user@outlook.com","givenName":"Pat","surname":"Kim"}""",
                System.Text.Encoding.UTF8,
                "application/json")
        });

        var sut = CreateService(handler);
        var account = await sut.GetAsync(Token, CancellationToken.None);

        Assert.Equal("ms-id", account.Identity);
        Assert.Equal("user@outlook.com", account.Email.Address);
        Assert.Equal("Pat", account.FirstName);
        Assert.Equal("Kim", account.LastName);
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
                Content = new StringContent(
                    """{"id":"1","userPrincipalName":"x@y.com"}""",
                    System.Text.Encoding.UTF8,
                    "application/json")
            });
        });

        var sut = CreateService(handler);
        await sut.GetAsync(Token, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal("/v1.0/users/me", captured.RequestUri?.PathAndQuery);
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
            Content = new StringContent("<<<", System.Text.Encoding.UTF8, "application/json")
        });

        var sut = CreateService(handler);

        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() => sut.GetAsync(Token, CancellationToken.None));
    }
}
