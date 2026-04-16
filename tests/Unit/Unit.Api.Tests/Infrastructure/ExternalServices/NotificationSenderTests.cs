using System.Net;
using System.Net.Mime;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using People.Infrastructure.Email.Models;
using People.Infrastructure.EmailBuilder;
using People.Infrastructure.Providers;
using People.Domain.ValueObjects;
using Xunit;

namespace Unit.Api.Tests.Infrastructure.ExternalServices;

public sealed class NotificationSenderTests
{
    private static NotificationSender CreateService(MockHttpMessageHandler handler, IEmailBuilder emailBuilder) =>
        new(new HttpClient(handler, disposeHandler: true) { BaseAddress = new Uri("https://notify.test/") },
            emailBuilder,
            NullLogger<NotificationSender>.Instance);

    [Fact]
    public async Task SendConfirmationAsync_PostsHtmlBodyAndSubjectQuery()
    {
        var emailBuilder = Substitute.For<IEmailBuilder>();
        emailBuilder
            .CreateEmailAsync("Confirmation.en.liquid", Arg.Any<ConfirmationCodeModel>())
            .Returns(new EmailTemplateResult("Verify your email", "<html><body>code</body></html>"));

        Uri? requestUri = null;
        HttpMethod? method = null;
        string? mediaType = null;
        string? body = null;
        var handler = new MockHttpMessageHandler();
        handler.Configure(async (req, _) =>
        {
            method = req.Method;
            requestUri = req.RequestUri;
            if (req.Content is not null)
            {
                mediaType = req.Content.Headers.ContentType?.MediaType;
                body = await req.Content.ReadAsStringAsync();
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var sut = CreateService(handler, emailBuilder);
        await sut.SendConfirmationAsync(
            new System.Net.Mail.MailAddress("user+tag@example.com"),
            "secret-code",
            Locale.Parse("en"),
            CancellationToken.None);

        Assert.Equal(HttpMethod.Post, method);

        var expectedPath = QueryHelpers.AddQueryString(
            $"/emails/{Uri.EscapeDataString("user+tag@example.com")}",
            "subject",
            "Verify your email");
        Assert.Equal(new Uri($"https://notify.test{expectedPath}"), requestUri);

        Assert.Equal(MediaTypeNames.Text.Html, mediaType);
        Assert.Equal("<html><body>code</body></html>", body);

        await emailBuilder.Received(1).CreateEmailAsync(
            "Confirmation.en.liquid",
            Arg.Is<ConfirmationCodeModel>(m => m.Code == "secret-code"));
    }

    [Fact]
    public async Task SendConfirmationAsync_ServerError_ThrowsHttpRequestException()
    {
        var emailBuilder = Substitute.For<IEmailBuilder>();
        emailBuilder
            .CreateEmailAsync(Arg.Any<string>(), Arg.Any<ITemplateModel>())
            .Returns(new EmailTemplateResult("s", "b"));

        var handler = new MockHttpMessageHandler();
        handler.ConfigureResponse(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var sut = CreateService(handler, emailBuilder);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            sut.SendConfirmationAsync(
                new System.Net.Mail.MailAddress("a@b.com"),
                "c",
                Locale.Parse("en"),
                CancellationToken.None));
    }

    [Fact]
    public async Task SendConfirmationAsync_NetworkFailure_PropagatesException()
    {
        var emailBuilder = Substitute.For<IEmailBuilder>();
        emailBuilder
            .CreateEmailAsync(Arg.Any<string>(), Arg.Any<ITemplateModel>())
            .Returns(new EmailTemplateResult("s", "b"));

        var handler = new MockHttpMessageHandler();
        handler.Configure((_, _) => throw new HttpRequestException("Simulated network failure"));

        var sut = CreateService(handler, emailBuilder);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() =>
            sut.SendConfirmationAsync(
                new System.Net.Mail.MailAddress("a@b.com"),
                "c",
                Locale.Parse("en"),
                CancellationToken.None));

        Assert.Equal("Simulated network failure", ex.Message);
    }
}
