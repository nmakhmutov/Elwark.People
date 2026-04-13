using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Fluid;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using People.Api.Endpoints;
using People.Api.Grpc;
using People.Api.Infrastructure;
using People.Api.Infrastructure.Interceptors;
using People.Application.Behaviour;
using People.Application.Queries.GetAccountSummary;
using People.Application.Webhooks;
using People.Domain.Entities;
using People.Domain.ValueObjects;
using People.Infrastructure;
using Scalar.AspNetCore;

const string appName = "People.Api";
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration.GetUri("Authentication:Authority").AbsoluteUri;
        options.Audience = builder.Configuration.GetString("Authentication:Audience");
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            NameClaimType = "sub",
            ClockSkew = TimeSpan.FromSeconds(10)
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                if (context.Principal?.Identity is not ClaimsIdentity identity)
                    return Task.CompletedTask;

                var scope = identity.FindFirst("scope");
                if (scope is null)
                    return Task.CompletedTask;

                identity.RemoveClaim(scope);

                var claims = scope.Value.Split(" ")
                    .Select(s => new Claim("scope", s));

                identity.AddClaims(claims);

                return Task.CompletedTask;
            }
        };
    });

builder.Services
    .AddAuthorizationBuilder()
    .AddPolicy(Policy.RequireRead.Name, Policy.RequireRead.Policy)
    .AddPolicy(Policy.RequireWrite.Name, Policy.RequireWrite.Policy)
    .AddPolicy(Policy.RequireAdmin.Name, Policy.RequireAdmin.Policy);

builder.Services
    .AddOpenApi()
    .AddCors()
    .AddLocalization(options => options.ResourcesPath = "Resources")
    .AddRequestLocalization(options =>
    {
        var cultures = new CultureInfo[]
        {
            new("en"),
            new("ru")
        };

        options.DefaultRequestCulture = new RequestCulture("en");
        options.SupportedCultures = cultures;
        options.SupportedUICultures = cultures;
        options.RequestCultureProviders = new List<IRequestCultureProvider>
        {
            new QueryStringRequestCultureProvider
            {
                QueryStringKey = "language",
                UIQueryStringKey = "language"
            },
            new AcceptLanguageHeaderRequestCultureProvider()
        };
    });

builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<PeopleDbContext>("people-db", tags: ["ready"])
    .AddDbContextCheck<WebhookDbContext>("webhook-db", tags: ["ready"]);

builder.Services
    .AddMediator(options =>
    {
        options.ServiceLifetime = ServiceLifetime.Scoped;
        options.PipelineBehaviors = [typeof(RequestLoggingBehavior<,>), typeof(RequestValidatorBehavior<,>)];
    })
    .AddValidatorsFromAssemblies(AppDomain.CurrentDomain.GetAssemblies(), ServiceLifetime.Scoped, null, true);

builder.AddInfrastructure()
    .AddFluid(options =>
    {
        options.ViewsFileProvider = builder.Environment.ContentRootFileProvider;
        options.TemplateOptions.MemberAccessStrategy = UnsafeMemberAccessStrategy.Instance;
    });

builder.Services
    .ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
        options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.SerializerOptions.PropertyNameCaseInsensitive = true;
    })
    .AddExceptionHandler<GlobalExceptionHandler>()
    .AddProblemDetails()
    .AddSingleton<IProblemDetailsFactory, ProblemDetailsFactory>();

builder.Services
    .AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<BrotliCompressionProvider>();
        options.Providers.Add<GzipCompressionProvider>();
    })
    .Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal)
    .Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal);

builder.Services
    .AddGrpc(options => options.Interceptors.Add<GrpcExceptionInterceptor>());

builder.AddSerilog(appName, configuration => configuration
    .Destructure.AsScalar<AccountId>()
    .Destructure.AsScalar<Locale>()
    .Destructure.AsScalar<RegionCode>()
    .Destructure.AsScalar<CountryCode>()
    .Destructure.AsScalar<Timezone>()
    .Destructure.AsScalar<IPAddress>()
    .Destructure.ByTransforming<Account>(x => new { x.Id, x.Name.Nickname })
    .Destructure.ByTransforming<AccountSummary>(x => new { x.Id, x.Name.Nickname })
);

await using var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var peopleContext = scope.ServiceProvider.GetRequiredService<PeopleDbContext>();
    await peopleContext.Database.MigrateAsync();

    var webhookContext = scope.ServiceProvider.GetRequiredService<WebhookDbContext>();
    await webhookContext.Database.MigrateAsync();

    if (!builder.Environment.IsProduction())
    {
        const string url = "http://localhost:5011/webhooks/people";
        if (!await webhookContext.Consumers.AnyAsync(x => x.DestinationUrl == url))
        {
            var consumer = WebhookConsumer.Create(WebhookType.Updated, WebhookMethod.Post, url, "webhook-secret");
            await webhookContext.Consumers.AddAsync(consumer);
        }
    }
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        ForwardLimit = 10
    })
    .UseCors(policy => policy
        .WithOrigins(builder.Configuration.GetRequiredSection("Cors").Get<string[]>()!)
        .WithMethods(HttpMethods.Get, HttpMethods.Post, HttpMethods.Put, HttpMethods.Delete)
        .AllowAnyHeader()
        .AllowCredentials()
    )
    .UseRequestLocalization()
    .UseExceptionHandler()
    .UseAuthentication()
    .UseAuthorization();

if (app.Environment.IsProduction())
{
    app.UseResponseCompression();
}
else
{
    app.MapOpenApi();
    app.MapScalarApiReference("/docs");
}

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = static _ => false,
    ResponseWriter = static (context, report) => context.Response.WriteAsJsonAsync(report)
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = static check => check.Tags.Contains("ready"),
    ResponseWriter = static (context, report) => context.Response.WriteAsJsonAsync(report)
});

app.MapAccountEndpoints();
app.MapDictionariesEndpoints();
app.MapWebhookEndpoints();

app.MapGrpcService<PeopleService>();

await app.RunAsync();
