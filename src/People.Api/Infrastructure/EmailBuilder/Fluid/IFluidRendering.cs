using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace People.Api.Infrastructure.EmailBuilder.Fluid
{
    public interface IFluidRendering
    {
        Task<string> RenderAsync(string path, object model, ViewDataDictionary viewData,
            ModelStateDictionary modelState);
    }
}