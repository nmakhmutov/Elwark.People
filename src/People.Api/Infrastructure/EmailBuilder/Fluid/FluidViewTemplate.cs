using Fluid;
using People.Api.Infrastructure.EmailBuilder.Fluid.Tags;

namespace People.Api.Infrastructure.EmailBuilder.Fluid
{
    public class FluidViewTemplate : BaseFluidTemplate<FluidViewTemplate>
    {
        static FluidViewTemplate()
        {
            Factory.RegisterTag<LayoutTag>("layout");
            Factory.RegisterTag<RenderBodyTag>("renderbody");
            Factory.RegisterBlock<RegisterSectionBlock>("section");
            Factory.RegisterTag<RenderSectionTag>("rendersection");
        }
    }
}