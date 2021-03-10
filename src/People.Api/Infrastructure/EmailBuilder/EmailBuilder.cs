using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using People.Api.Infrastructure.EmailBuilder.Fluid;

namespace People.Api.Infrastructure.EmailBuilder
{
    public class EmailBuilder : IEmailBuilder
    {
        private readonly Regex _emailTitleRegex = new(@"(?<=<title.*>)([\s\S]*)(?=</title>)");
        private readonly ModelStateDictionary _modelState;
        private readonly IFluidRendering _rendering;
        private readonly ViewDataDictionary _viewData;

        public EmailBuilder(IFluidRendering rendering)
        {
            _rendering = rendering;
            _modelState = new ModelStateDictionary();
            _viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            {
                {"Logo", "https://res.cloudinary.com/elwark/image/upload/v1613019548/Elwark/white/60x60_g4vyxq.png"}
            };
        }

        public async Task<EmailTemplateResult> CreateEmailAsync(string templateName, ITemplateModel model)
        {
            var body = await _rendering.RenderAsync(CreatePath(templateName), model, _viewData, _modelState);

            var subject = _emailTitleRegex.Match(body).Value.Trim();

            return new EmailTemplateResult(subject, body);
        }

        private static string CreatePath(string template) => $"Email/Views/{template}";
    }
}