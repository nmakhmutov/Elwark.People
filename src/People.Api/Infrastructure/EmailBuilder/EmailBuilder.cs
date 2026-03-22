using System.Text.RegularExpressions;
using Fluid;
using Fluid.ViewEngine;

namespace People.Api.Infrastructure.EmailBuilder;

internal sealed partial class EmailBuilder : IEmailBuilder
{
    private readonly IFluidViewRenderer _rendering;
    private readonly ILogger<EmailBuilder> _logger;
    private readonly string _host;

    public EmailBuilder(IFluidViewRenderer rendering, IConfiguration configuration, ILogger<EmailBuilder> logger)
    {
        _rendering = rendering;
        _logger = logger;
        _host = configuration["Authentication:Authority"] ?? "https://identity.elwark.com";
    }

    public async Task<EmailTemplateResult> CreateEmailAsync(string templateName, ITemplateModel model)
    {
        LogCreatingEmail(templateName);

        var context = new TemplateContext(model);
        context.SetValue("IdentityHost", _host);

        await using var writer = new StringWriter();
        await _rendering.RenderViewAsync(writer, $"Email/Views/{templateName}", context);

        await writer.FlushAsync();

        var body = writer.ToString();
        var subject = GetHtmlTitleRegex().Match(body).Value.Trim();

        LogCreatedEmail(templateName, subject);

        return new EmailTemplateResult(subject, body);
    }

    [GeneratedRegex("(?<=<title.*>)([\\s\\S]*)(?=</title>)")]
    private static partial Regex GetHtmlTitleRegex();

    [LoggerMessage(LogLevel.Information, "Creating email template {template}")]
    partial void LogCreatingEmail(string template);

    [LoggerMessage(LogLevel.Information, "Created email template {template} with subject {subject}")]
    partial void LogCreatedEmail(string template, string subject);
}
