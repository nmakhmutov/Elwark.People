using Fluid;
using People.Worker.TemplateViewEngine.Tags;

namespace People.Worker.TemplateViewEngine
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