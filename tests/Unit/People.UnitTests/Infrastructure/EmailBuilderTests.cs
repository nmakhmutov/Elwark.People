using Fluid;
using Fluid.ViewEngine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using People.Infrastructure.Email.Models;
using People.Infrastructure.EmailBuilder;
using Xunit;

namespace People.UnitTests.Infrastructure;

public sealed class EmailBuilderTests
{
    [Fact]
    public async Task CreateEmailAsync_ExtractsSubjectFromTitleTag_AndReturnsNonEmptySubjectAndBody()
    {
        const string html =
            """<!DOCTYPE html><html><head><title>  Confirm your address  </title></head><body><p>Hi</p></body></html>""";

        var renderer = Substitute.For<IFluidViewRenderer>();
        renderer
            .RenderViewAsync(Arg.Any<TextWriter>(), Arg.Any<string>(), Arg.Any<TemplateContext>())
            .Returns(callInfo =>
            {
                callInfo.ArgAt<TextWriter>(0).Write(html);
                return Task.CompletedTask;
            });

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Authority"] = "https://auth.test"
            })
            .Build();

        var sut = new EmailBuilder(renderer, configuration, NullLogger<EmailBuilder>.Instance);

        var result = await sut.CreateEmailAsync("Confirmation.en.liquid", new ConfirmationCodeModel("abc"));

        Assert.False(string.IsNullOrWhiteSpace(result.Subject));
        Assert.Equal("Confirm your address", result.Subject);
        Assert.False(string.IsNullOrWhiteSpace(result.Body));
        Assert.Contains("<title>", result.Body, StringComparison.Ordinal);
        Assert.Contains("Confirm your address", result.Body, StringComparison.Ordinal);

        await renderer.Received(1).RenderViewAsync(
            Arg.Any<TextWriter>(),
            "Email/Views/Confirmation.en.liquid",
            Arg.Any<TemplateContext>());
    }

    [Fact]
    public async Task CreateEmailAsync_UsesDefaultIdentityHost_WhenAuthorityMissing()
    {
        var renderer = Substitute.For<IFluidViewRenderer>();
        renderer
            .RenderViewAsync(Arg.Any<TextWriter>(), Arg.Any<string>(), Arg.Any<TemplateContext>())
            .Returns(callInfo =>
            {
                callInfo.ArgAt<TextWriter>(0).Write("<html><title>T</title></html>");
                return Task.CompletedTask;
            });

        var configuration = new ConfigurationBuilder().Build();
        var sut = new EmailBuilder(renderer, configuration, NullLogger<EmailBuilder>.Instance);

        _ = await sut.CreateEmailAsync("x.liquid", new ConfirmationCodeModel("1"));

        await renderer.Received(1).RenderViewAsync(
            Arg.Any<TextWriter>(),
            "Email/Views/x.liquid",
            Arg.Any<TemplateContext>());
    }
}
