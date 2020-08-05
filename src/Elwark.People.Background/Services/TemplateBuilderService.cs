using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Elwark.People.Background.Models;
using Elwark.People.Background.TemplateViewEngine;
using Elwark.Storage.Client;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Elwark.People.Background.Services
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
        private readonly Regex _emailTitleRegex = new Regex(@"(?<=<title.*>)([\s\S]*)(?=</title>)");
        private readonly ModelStateDictionary _modelState;
        private readonly IFluidRendering _rendering;
        private readonly ViewDataDictionary _viewData;

        public TemplateBuilderService(IElwarkStorageClient staticClient, IFluidRendering rendering)
        {
            _rendering = rendering;
            _modelState = new ModelStateDictionary();
            _viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            {
                {"Logo", staticClient.Static.Icons.Elwark.Primary.Size48x48.Path.ToString()}
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