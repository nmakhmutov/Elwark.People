using System.Text.RegularExpressions;
using Fluid;
using Fluid.ViewEngine;

namespace People.Api.Infrastructure.EmailBuilder;

internal sealed partial class EmailBuilder : IEmailBuilder
{
    private readonly IFluidViewRenderer _rendering;

    public EmailBuilder(IFluidViewRenderer rendering) =>
        _rendering = rendering;

    public async Task<EmailTemplateResult> CreateEmailAsync(string templateName, ITemplateModel model)
    {
        await using var writer = new StringWriter();
        await _rendering.RenderViewAsync(writer, $"Email/Views/{templateName}", new TemplateContext(model));

        await writer.FlushAsync();

        var body = writer.ToString();
        var subject = GetHtmlTitleRegex().Match(body).Value.Trim();

        return new EmailTemplateResult(subject, body);
    }

    [GeneratedRegex("(?<=<title.*>)([\\s\\S]*)(?=</title>)", RegexOptions.NonBacktracking)]
    private static partial Regex GetHtmlTitleRegex();
}
