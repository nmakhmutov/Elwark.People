using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mime;
using CorrelationId;
using CorrelationId.DependencyInjection;
using CorrelationId.HttpClient;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using People.Gateway.Infrastructure;
using People.Gateway.Infrastructure.Identity;
using People.Host;

namespace People.Gateway
{
    public class Startup
    {
        private const string MainCors = "MainCORS";

        public Startup(IConfiguration configuration) =>
            Configuration = configuration;

        private IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2Support", true);
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Remove("sub");

            services.AddCorrelationId(options =>
                {
                    options.UpdateTraceIdentifier = true;
                    options.AddToLoggingScope = true;
                })
                .WithTraceIdentifierProvider();

            services.AddCors(options =>
            {
                options.AddPolicy(MainCors, builder => builder
                    .WithOrigins(Configuration.GetSection("Urls:Cors").Get<string[]>())
                    .WithMethods(HttpMethods.Get, HttpMethods.Post, HttpMethods.Put, HttpMethods.Delete)
                    .AllowAnyHeader()
                );
            });

            services.AddAuthorizationCore(options =>
            {
                options.AddPolicy(Policy.RequireAccountId, Policy.RequireAccountIdPolicy());
                options.AddPolicy(Policy.RequireCommonAccess, Policy.RequireCommonAccessPolicy());
                options.AddPolicy(Policy.RequireProfileAccess, Policy.RequireProfileAccessPolicy());
            });

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.Authority = Configuration["Urls:Identity"];
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        NameClaimType = "name",
                        RoleClaimType = "role"
                    };
                });

            services.AddHttpContextAccessor()
                .AddScoped<IIdentityService, IdentityService>()
                .AddTransient<LocalizationMessageHandler>();

            services.AddGrpcClient<Grpc.Gateway.Gateway.GatewayClient>(
                    options => options.Address = new Uri(Configuration["Urls:People.Api"])
                )
                .AddHttpMessageHandler<LocalizationMessageHandler>()
                .AddCorrelationIdForwarding();

            services.AddControllers(options =>
                {
                    options.AllowEmptyInputInBodyModelBinding = false;
                    options.Filters.Add<HttpGlobalExceptionFilter>();
                })
                .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = context =>
                {
                    var details = ErrorFactory.Create(context.ModelState);

                    var result = new BadRequestObjectResult(details);
                    result.ContentTypes.Add(MediaTypeNames.Application.Json);
                    result.ContentTypes.Add(MediaTypeNames.Application.Xml);

                    return result;
                })
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                    options.SerializerSettings.Formatting = Formatting.None;
                    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    options.SerializerSettings.Converters = new JsonConverter[]
                    {
                        new IsoDateTimeConverter(),
                        new StringEnumConverter(new CamelCaseNamingStrategy())
                    };
                    options.SerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
                    options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                })
                .AddFluentValidation(configuration =>
                    configuration.RegisterValidatorsFromAssemblies(AppDomain.CurrentDomain.GetAssemblies())
                );
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseForwardedHeaders(new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.All
                })
                .UseCors(MainCors)
                .UseCorrelationId()
                .UseRouting()
                .UseAuthentication()
                .UseAuthorization()
                .UsePeopleLocalization()
                .UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}
