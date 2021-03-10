using Microsoft.Extensions.Options;

namespace People.Api.Infrastructure.EmailBuilder.Fluid
{
    public class FluidViewEngineOptionsSetup : ConfigureOptions<FluidViewEngineOptions>
    {
        public FluidViewEngineOptionsSetup()
            : base(_ => { })
        {
        }
    }
}