using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using People.Infrastructure.Providers;
using Xunit;

namespace People.UnitTests.Infrastructure.ExternalServices;

public sealed class GravatarServiceTests
{
    private static string Md5Hex(string email)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(email));
        return string.Concat(hash.Select(b => b.ToString("x2")));
    }

    private static GravatarService CreateService(MockHttpMessageHandler handler) =>
        new(new HttpClient(handler, disposeHandler: true) { BaseAddress = new Uri("https://gravatar.test/") },
            NullLogger<GravatarService>.Instance);

    [Fact]
    public async Task GetAsync_ValidJson_ReturnsGravatarProfile()
    {
        var email = new MailAddress("User@Example.com");
        var hash = Md5Hex(email.Address);

        var handler = new MockHttpMessageHandler();
        handler.Configure((req, _) =>
        {
            var pathAndQuery = req.RequestUri?.PathAndQuery;
            Assert.Equal($"/{hash}.json", pathAndQuery);
            const string json =
                """
                {"entry":[{"preferredUsername":"pu","thumbnailUrl":"https://t.example/a.png",
                 "aboutMe":"bio","name":[{"givenName":"F","familyName":"L"}]}]}
                """;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        });

        var sut = CreateService(handler);
        var profile = await sut.GetAsync(email);

        Assert.NotNull(profile);
        Assert.Equal("pu", profile.PreferredUsername);
        Assert.Equal("https://t.example/a.png", profile.ThumbnailUrl);
        Assert.Equal("bio", profile.AboutMe);
        Assert.NotNull(profile.Name);
        Assert.Single(profile.Name!);
        Assert.Equal("F", profile.Name![0].FirstName);
        Assert.Equal("L", profile.Name![0].LastName);
    }

    [Fact]
    public async Task GetAsync_ProfileNotFound_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler();
        handler.ConfigureResponse(new HttpResponseMessage(HttpStatusCode.NotFound));

        var sut = CreateService(handler);
        var profile = await sut.GetAsync(new MailAddress("nobody@example.com"));

        Assert.Null(profile);
    }

    [Fact]
    public async Task GetAsync_ServerError_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler();
        handler.ConfigureResponse(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var sut = CreateService(handler);
        var profile = await sut.GetAsync(new MailAddress("a@b.com"));

        Assert.Null(profile);
    }

    [Fact]
    public async Task GetAsync_MalformedJson_ReturnsNull()
    {
        var handler = new MockHttpMessageHandler();
        handler.ConfigureResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{broken", Encoding.UTF8, "application/json")
        });

        var sut = CreateService(handler);
        var profile = await sut.GetAsync(new MailAddress("a@b.com"));

        Assert.Null(profile);
    }

    [Fact]
    public async Task GetAsync_RequestsUrlWithMd5HashOfEmailAddress()
    {
        const string address = "hash-me@example.org";
        var expectedHash = Md5Hex(address);

        HttpRequestMessage? captured = null;
        var handler = new MockHttpMessageHandler();
        handler.Configure((req, _) =>
        {
            captured = req;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        var sut = CreateService(handler);
        await sut.GetAsync(new MailAddress(address));

        Assert.NotNull(captured);
        Assert.Equal($"https://gravatar.test/{expectedHash}.json", captured.RequestUri?.ToString());
    }
}
