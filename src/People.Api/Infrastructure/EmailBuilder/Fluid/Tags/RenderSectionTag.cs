using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Fluid;
using Fluid.Ast;
using Fluid.Tags;

namespace People.Api.Infrastructure.EmailBuilder.Fluid.Tags
{
    public class RenderSectionTag : IdentifierTag
    {
        public override async ValueTask<Completion> WriteToAsync(TextWriter writer, TextEncoder encoder,
            TemplateContext context, string sectionName)
        {
            if (!context.AmbientValues.TryGetValue("Sections", out var sections)) 
                return Completion.Normal;
            
            var dictionary = sections as Dictionary<string, List<Statement>> ??
                             new Dictionary<string, List<Statement>>();
            
            if (!dictionary.TryGetValue(sectionName, out var section)) 
                return Completion.Normal;
            
            foreach (var statement in section)
                await statement.WriteToAsync(writer, encoder, context);

            return Completion.Normal;
        }
    }
}