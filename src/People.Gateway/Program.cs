using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mime;
using CorrelationId;
using CorrelationId.DependencyInjection;
using CorrelationId.HttpClient;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using People.Gateway.Infrastructure;
using People.Gateway.Infrastructure.Identity;
using People.Grpc.Gateway;
using People.Host;
using Serilog;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using JsonConverter = Newtonsoft.Json.JsonConverter;

const string appName = "People.Gateway";
const string mainCors = "MainCORS";
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
var configuration = HostExtensions.CreateConfiguration(environment, args);

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2Support", true);
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Remove("sub");

Log.Logger = HostExtensions.CreateLogger(configuration, appName);

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddConfiguration(configuration);

builder.Services.AddCorrelationId(options =>
    {
        options.UpdateTraceIdentifier = true;
        options.AddToLoggingScope = true;
    })
    .WithTraceIdentifierProvider();

builder.Services.AddCors(options =>
    options.AddPolicy(mainCors, policyBuilder => policyBuilder
        .WithOrigins(configuration.GetSection("Urls:Cors").Get<string[]>())
        .WithMethods(HttpMethods.Get, HttpMethods.Post, HttpMethods.Put, HttpMethods.Delete)
        .AllowAnyHeader()
    ));

builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy(Policy.RequireAccountId, Policy.RequireAccountIdPolicy());
    options.AddPolicy(Policy.RequireCommonAccess, Policy.RequireCommonAccessPolicy());
    options.AddPolicy(Policy.RequireProfileAccess, Policy.RequireProfileAccessPolicy());
});

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Authority = configuration["Urls:Identity"];
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

builder.Services
    .AddHttpContextAccessor()
    .AddScoped<IIdentityService, IdentityService>()
    .AddTransient<LocalizationMessageHandler>();

builder.Services
    .AddGrpcClient<Gateway.GatewayClient>(options => options.Address = new Uri(configuration["Urls:People.Api"]))
    .AddHttpMessageHandler<LocalizationMessageHandler>()
    .AddCorrelationIdForwarding();

builder.Services.AddControllers(options =>
    {
        options.AllowEmptyInputInBodyModelBinding = false;
        options.Filters.Add<GlobalExceptionFilter>();
    })
    .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = context =>
    {
        var result = new BadRequestObjectResult(ErrorFactory.Create(context.ModelState));
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
    .AddFluentValidation(x => x.RegisterValidatorsFromAssemblies(AppDomain.CurrentDomain.GetAssemblies()));

builder.Host.UseSerilog();

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.All
    })
    .UseCors(mainCors)
    .UseCorrelationId()
    .UseRouting()
    .UseAuthentication()
    .UseAuthorization()
    .UsePeopleLocalization();

app.MapControllers();

app.Run();
