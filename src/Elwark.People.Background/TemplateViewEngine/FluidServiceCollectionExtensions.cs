using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elwark.People.Background.TemplateViewEngine
{
    public static class FluidServiceCollectionExtensions
    {
        public static IServiceCollection AddFluid(this IServiceCollection services,
            Action<FluidViewEngineOptions>? setupAction = null)
        {
            if (services is null)
                throw new ArgumentNullException(nameof(services));

            services.AddOptions();
            services.AddTransient<IConfigureOptions<FluidViewEngineOptions>, FluidViewEngineOptionsSetup>();

            if (setupAction is {})
                services.Configure(setupAction);

            services.AddSingleton<IFluidRendering, FluidRendering>();
            return services;
        }
    }
}