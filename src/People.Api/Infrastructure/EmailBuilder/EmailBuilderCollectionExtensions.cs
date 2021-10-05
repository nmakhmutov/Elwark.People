using System;
using Fluid.MvcViewEngine;
using Fluid.ViewEngine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace People.Api.Infrastructure.EmailBuilder;

public static class EmailBuilderCollectionExtensions
{
    public static IServiceCollection AddEmailBuilder(this IServiceCollection services,
        Action<FluidViewEngineOptions>? setupAction = null)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        if (setupAction is not null)
            services.Configure(setupAction);

        services
            .AddOptions()
            .AddMemoryCache()
            .AddSingleton<IEmailBuilder, EmailBuilder>()
            .AddSingleton<IFluidRendering, FluidRendering>()
            .AddSingleton<IFluidViewEngine, FluidViewEngine>()
            .AddTransient<IConfigureOptions<FluidViewEngineOptions>, FluidViewEngineOptionsSetup>();

        return services;
    }
}
