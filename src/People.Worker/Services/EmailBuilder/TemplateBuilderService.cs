using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using People.Worker.TemplateViewEngine;

namespace People.Worker.Services.EmailBuilder
{
    public interface ITemplateModel
    {
    }

    public interface ITemplateBuilderService
    {
        Task<EmailTemplateResult> CreateEmailAsync(string templateName, ITemplateModel? model);
    }

    public class TemplateBuilderService : ITemplateBuilderService
    {
        private readonly Regex _emailTitleRegex = new(@"(?<=<title.*>)([\s\S]*)(?=</title>)");
        private readonly ModelStateDictionary _modelState;
        private readonly IFluidRendering _rendering;
        private readonly ViewDataDictionary _viewData;

        public TemplateBuilderService(IFluidRendering rendering)
        {
            _rendering = rendering;
            _modelState = new ModelStateDictionary();
            _viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            {
                {"Logo", "logo"}
            };
        }

        public async Task<EmailTemplateResult> CreateEmailAsync(string templateName, ITemplateModel? model)
        {
            var body = await _rendering.RenderAsync(
                CreatePath(templateName), model ?? new object(), _viewData, _modelState
            );

            var subject = _emailTitleRegex.Match(body).Value.Trim();

            return new EmailTemplateResult(subject, body);
        }

        private static string CreatePath(string template) => $"Templates/{template}";
    }
}