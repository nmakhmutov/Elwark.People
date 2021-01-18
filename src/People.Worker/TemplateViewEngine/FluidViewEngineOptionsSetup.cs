using Microsoft.Extensions.Options;

namespace People.Worker.TemplateViewEngine
{
    public class FluidViewEngineOptionsSetup : ConfigureOptions<FluidViewEngineOptions>
    {
        public FluidViewEngineOptionsSetup()
            : base(_ => { })
        {
        }
    }
}