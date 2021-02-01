using System;
using CorrelationId;
using CorrelationId.DependencyInjection;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using People.Api.Application.Behaviors;
using People.Api.Grpc;
using People.Api.Infrastructure.Interceptors;
using People.Api.Infrastructure.Password;
using People.Api.Infrastructure.Providers.Google;
using People.Api.Infrastructure.Providers.Microsoft;
using People.Domain.AggregateModels.Account;
using People.Infrastructure;
using People.Infrastructure.Confirmations;
using People.Infrastructure.Forbidden;
using People.Infrastructure.Repositories;
using People.Infrastructure.Sequences;

namespace People.Api
{
    public class Startup
    {
        private IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration) =>
            Configuration = configuration;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions()
                .AddCorrelationId(options =>
                {
                    options.UpdateTraceIdentifier = true;
                    options.AddToLoggingScope = true;
                })
                .WithTraceIdentifierProvider();

            services.Configure<DbContextSettings>(Configuration.GetSection("Mongodb"))
                .AddScoped<PeopleDbContext>()
                .AddScoped<InfrastructureDbContext>()
                .AddScoped<IForbiddenService, ForbiddenService>()
                .AddScoped<ISequenceGenerator, SequenceGenerator>()
                .AddScoped<IAccountRepository, AccountRepository>();

            services
                .Configure<PasswordValidationOptions>(Configuration.GetSection("PasswordValidation"))
                .AddScoped<IPasswordValidator, PasswordValidator>()
                .AddSingleton<IPasswordHasher>(_ => new PasswordHasher(Configuration["App:Key"]));

            services.AddScoped<IConfirmationService, ConfirmationService>();
            
            services.AddHttpClient<IGoogleApiService, GoogleApiService>(client =>
                client.BaseAddress = new Uri(Configuration["Urls:GoogleApi"])
            );

            services.AddHttpClient<IMicrosoftApiService, MicrosoftApiService>(client =>
                client.BaseAddress = new Uri(Configuration["Urls:MicrosoftApi"])
            );
            
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            services.AddMediatR(assemblies)
                .AddValidatorsFromAssemblies(assemblies)
                .AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>))
                .AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidatorBehavior<,>));

            services.AddGrpc(options => options.Interceptors.Add<GlobalErrorInterceptor>());
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseForwardedHeaders(new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                })
                .UseCorrelationId()
                .UseRouting()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapGrpcService<GatewayService>();
                    endpoints.MapGrpcService<IdentityService>();

                    endpoints.MapGet("/",
                        async context =>
                        {
                            await context.Response.WriteAsync(
                                "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                        });
                });
        }
    }
}