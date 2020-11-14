using Microsoft.Extensions.Options;

namespace Elwark.People.Background.TemplateViewEngine
{
    public class FluidViewEngineOptionsSetup : ConfigureOptions<FluidViewEngineOptions>
    {
        public FluidViewEngineOptionsSetup()
            : base(_ => { })
        {
        }
    }
}