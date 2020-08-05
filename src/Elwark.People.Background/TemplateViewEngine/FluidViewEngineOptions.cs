using System.Text.Encodings.Web;
using Microsoft.Extensions.FileProviders;

namespace Elwark.People.Background.TemplateViewEngine
{
    public class FluidViewEngineOptions
    {
        public const string ViewExtension = ".liquid";

        /// <summary>
        ///     Gets or sets the text encoder to use during rendering.
        /// </summary>
        public TextEncoder TextEncoder = HtmlEncoder.Default;

        public IFileProvider? FileProvider { get; set; }
    }
}