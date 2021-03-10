using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using People.Api.Infrastructure.EmailBuilder.Fluid;

namespace People.Api.Infrastructure.EmailBuilder
{
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
                .AddTransient<IConfigureOptions<FluidViewEngineOptions>, FluidViewEngineOptionsSetup>()
                .AddOptions()
                .AddMemoryCache()
                .AddSingleton<IFluidRendering, FluidRendering>()
                .AddSingleton<IEmailBuilder, EmailBuilder>();
            
            return services;
        }
    }
}