using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Elwark.People.Background.TemplateViewEngine
{
    public interface IFluidRendering
    {
        Task<string> RenderAsync(string path, object model, ViewDataDictionary viewData,
            ModelStateDictionary modelState);
    }
}