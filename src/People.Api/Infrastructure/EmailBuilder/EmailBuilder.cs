using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fluid;
using Fluid.ViewEngine;

namespace People.Api.Infrastructure.EmailBuilder;

public sealed class EmailBuilder : IEmailBuilder
{
    private readonly Regex _emailTitleRegex = new(@"(?<=<title.*>)([\s\S]*)(?=</title>)");
    private readonly IFluidViewRenderer _rendering;

    public EmailBuilder(IFluidViewRenderer rendering) =>
        _rendering = rendering;

    public async Task<EmailTemplateResult> CreateEmailAsync(string templateName, ITemplateModel model)
    {
        await using var ms = new MemoryStream();
        await using var sw = new StreamWriter(ms);
        await _rendering.RenderViewAsync(sw, $"Email/Views/{templateName}", new TemplateContext(model));

        var body = Encoding.UTF8.GetString(ms.ToArray());
        var subject = _emailTitleRegex.Match(body).Value.Trim();

        return new EmailTemplateResult(subject, body);
    }
}
