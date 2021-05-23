using System;
using Fluid.MvcViewEngine;
using Fluid.ViewEngine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace People.Api.Infrastructure.EmailBuilder
{
    public static class EmailBuilderCollectionExtensions
    {
        public static IServiceCollection AddEmailBuilder(this IServiceCollection services,
            Action<FluidViewEngineOptions>? setupAction = null)
        {
            if (services is null)
                throw new ArgumentNullException(nameof(services));

            services
                .AddOptions()
                .AddMemoryCache()
                .AddSingleton<IEmailBuilder, EmailBuilder>()
                .AddSingleton<IFluidRendering, FluidRendering>()
                .AddTransient<IConfigureOptions<FluidViewEngineOptions>, FluidViewEngineOptionsSetup>();

            if (setupAction is not null)
                services.Configure(setupAction);

            return services;
        }
    }
}
