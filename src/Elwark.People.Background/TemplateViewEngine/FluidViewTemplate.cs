using Elwark.People.Background.TemplateViewEngine.Tags;
using Fluid;

namespace Elwark.People.Background.TemplateViewEngine
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