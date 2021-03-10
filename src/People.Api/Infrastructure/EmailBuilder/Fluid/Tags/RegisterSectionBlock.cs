using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Fluid;
using Fluid.Ast;
using Fluid.Tags;

namespace People.Api.Infrastructure.EmailBuilder.Fluid.Tags
{
    public class RegisterSectionBlock : IdentifierBlock
    {
        private static readonly ValueTask<Completion> Normal = new(Completion.Normal);

        public override ValueTask<Completion> WriteToAsync(TextWriter writer, TextEncoder encoder,
            TemplateContext context, string sectionName, List<Statement> statements)
        {
            if (context.AmbientValues.TryGetValue("Sections", out var sections))
            {
                var dictionary = sections as Dictionary<string, List<Statement>> ??
                                 new Dictionary<string, List<Statement>>();
                dictionary[sectionName] = statements;
            }

            return Normal;
        }
    }
}