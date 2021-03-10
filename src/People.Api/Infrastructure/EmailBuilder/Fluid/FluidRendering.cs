using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Fluid;
using Fluid.Ast;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace People.Api.Infrastructure.EmailBuilder.Fluid
{
    public class FluidRendering : IFluidRendering
    {
        private const string ViewStartFilename = "_ViewStart.liquid";
        public const string ViewPath = "ViewPath";
        private static readonly Func<IFluidTemplate> FluidTemplateFactory = () => new FluidViewTemplate();
        private readonly IHostEnvironment _hostEnvironment;
        private readonly IMemoryCache _memoryCache;
        private readonly FluidViewEngineOptions _options;

        static FluidRendering() =>
            TemplateContext.GlobalMemberAccessStrategy.Register<ModelStateDictionary>();

        public FluidRendering(IMemoryCache memoryCache, IOptions<FluidViewEngineOptions> optionsAccessor,
            IHostEnvironment hostEnvironment)
        {
            _memoryCache = memoryCache;
            _hostEnvironment = hostEnvironment;
            _options = optionsAccessor.Value;
        }

        public async Task<string> RenderAsync(string path, object model, ViewDataDictionary viewData,
            ModelStateDictionary modelState)
        {
            // Check for a custom file provider
            var fileProvider = _options.FileProvider ?? _hostEnvironment.ContentRootFileProvider;

            var template = ParseLiquidFile(path, fileProvider, true);

            var context = new TemplateContext();
            context.MemberAccessStrategy.Register(model.GetType());
            context.LocalScope.SetValue("Model", model);
            context.LocalScope.SetValue("ViewData", viewData);
            context.LocalScope.SetValue("ModelState", modelState);

            // Provide some services to all statements
            context.AmbientValues["FileProvider"] = fileProvider;
            context.AmbientValues[ViewPath] = path;
            context.AmbientValues["Sections"] = new Dictionary<string, List<Statement>>();
            context.ParserFactory = FluidViewTemplate.Factory;
            context.TemplateFactory = FluidTemplateFactory;
            context.FileProvider = new FileProviderMapper(fileProvider, "Email/Views");

            var body = await template.RenderAsync(context, _options.TextEncoder);

            // If a layout is specified while rendering a view, execute it
            if (context.AmbientValues.TryGetValue("Layout", out var layoutPath))
            {
                context.AmbientValues[ViewPath] = layoutPath;
                context.AmbientValues["Body"] = body;
                var layoutTemplate = ParseLiquidFile((string) layoutPath, fileProvider, false);

                return await layoutTemplate.RenderAsync(context, _options.TextEncoder);
            }

            return body;
        }

        private IEnumerable<string> FindViewStarts(string viewPath, IFileProvider fileProvider)
        {
            var viewStarts = new List<string>();
            var index = viewPath.Length - 1;
            while (!string.IsNullOrEmpty(viewPath) && !string.Equals(viewPath, "Email/Views", StringComparison.OrdinalIgnoreCase))
            {
                index = viewPath.LastIndexOf('/', index);

                if (index == -1) 
                    return viewStarts;

                viewPath = viewPath.Substring(0, index + 1) + ViewStartFilename;

                var viewStartInfo = fileProvider.GetFileInfo(viewPath);
                if (viewStartInfo.Exists) viewStarts.Add(viewPath);

                index -= 1;
            }

            return viewStarts;
        }

        private FluidViewTemplate ParseLiquidFile(string path, IFileProvider fileProvider, bool includeViewStarts) =>
            _memoryCache.GetOrCreate(path, viewEntry =>
            {
                var statements = new List<Statement>();

                // Default sliding expiration to prevent the entries for being kept indefinitely
                viewEntry.SlidingExpiration = TimeSpan.FromHours(1);

                var fileInfo = fileProvider.GetFileInfo(path);
                viewEntry.ExpirationTokens.Add(fileProvider.Watch(path));

                if (includeViewStarts)
                    // Add ViewStart files
                    foreach (var viewStartPath in FindViewStarts(path, fileProvider))
                    {
                        // Redefine the current view path while processing ViewStart files
                        statements.Add(new CallbackStatement((_, _, context) =>
                        {
                            context.AmbientValues[ViewPath] = viewStartPath;
                            return new ValueTask<Completion>(Completion.Normal);
                        }));

                        var viewStartTemplate = ParseLiquidFile(viewStartPath, fileProvider, false);

                        statements.AddRange(viewStartTemplate.Statements);
                    }

                using var stream = fileInfo.CreateReadStream();
                using var sr = new StreamReader(stream);
                if (!FluidViewTemplate.TryParse(sr.ReadToEnd(), out var template, out var errors))
                    throw new Exception(string.Join(Environment.NewLine, errors));
                
                statements.AddRange(template.Statements);
                template.Statements = statements;
                return template;
            });
    }
}