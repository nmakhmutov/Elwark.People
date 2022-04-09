using System;
using Fluid.ViewEngine;
using Microsoft.Extensions.DependencyInjection;

namespace People.Api.Infrastructure.EmailBuilder;

public static class EmailBuilderCollectionExtensions
{
    public static IServiceCollection AddEmailBuilder(this IServiceCollection services,
        Action<FluidViewEngineOptions>? setupAction = null)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        services
            .AddSingleton<IEmailBuilder, EmailBuilder>()
            .AddFluid(setupAction);

        return services;
    }
}
