using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mime;
using CorrelationId;
using CorrelationId.DependencyInjection;
using CorrelationId.HttpClient;
using FluentValidation.AspNetCore;
using People.Gateway.Infrastructure;
using People.Gateway.Infrastructure.Identity;
using Grpc.Core;
using Grpc.Net.Client.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using People.Gateway.Infrastructure.Extensions;
using People.Gateway.Infrastructure.Filters;
using People.Grpc.Gateway;
using People.Grpc.Infrastructure;
using People.Grpc.Notification;
using Serilog;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Display;

const string appName = "People.Gateway";
const string mainCors = "MainCORS";
// AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
// AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2Support", true);
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Remove("sub");

var builder = WebApplication.CreateBuilder(args);

var logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("application", appName)
    .WriteTo.Console(
        "json".Equals(builder.Configuration["Serilog:Formatter"])
            ? new CompactJsonFormatter()
            : new MessageTemplateTextFormatter(
                "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message}{NewLine}{Exception}")
    )
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Logging
    .ClearProviders()
    .AddSerilog(logger);

builder.Services.AddCorrelationId(options =>
    {
        options.UpdateTraceIdentifier = true;
        options.AddToLoggingScope = true;
    })
    .WithTraceIdentifierProvider();

builder.Services.AddCors(options =>
    options.AddPolicy(mainCors, policyBuilder => policyBuilder
        .WithOrigins(builder.Configuration.GetSection("Urls:Cors").Get<string[]>())
        .WithMethods(HttpMethods.Get, HttpMethods.Post, HttpMethods.Put, HttpMethods.Delete)
        .AllowAnyHeader()
    ));

builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy(Policy.RequireAccountId, Policy.RequireAccountIdPolicy());
    options.AddPolicy(Policy.RequireCommonAccess, Policy.RequireCommonAccessPolicy());
    options.AddPolicy(Policy.RequireProfileAccess, Policy.RequireProfileAccessPolicy());
    options.AddPolicy(Policy.ManagementAccess, Policy.ManagementAccessPolicy());
});

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Urls:Identity"];
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true
        };
    });

builder.Services
    .AddHttpContextAccessor()
    .AddScoped<IIdentityService, IdentityService>();

var defaultMethodConfig = new MethodConfig
{
    Names = { MethodName.Default },
    RetryPolicy = new RetryPolicy
    {
        MaxAttempts = 5,
        InitialBackoff = TimeSpan.FromSeconds(1),
        MaxBackoff = TimeSpan.FromSeconds(3),
        BackoffMultiplier = 1,
        RetryableStatusCodes = { StatusCode.Unavailable }
    }
};

builder.Services
    .AddGrpcClient<PeopleService.PeopleServiceClient>(options =>
    {
        options.Address = new Uri(builder.Configuration["Urls:People.Api"]);
        options.ChannelOptionsActions.Add(channel =>
        {
            channel.Credentials = ChannelCredentials.Insecure;
            channel.ServiceConfig = new ServiceConfig { MethodConfigs = { defaultMethodConfig } };
        });
    })
    .AddCorrelationIdForwarding();

builder.Services
    .AddGrpcClient<NotificationService.NotificationServiceClient>(options =>
    {
        options.Address = new Uri(builder.Configuration["Urls:Notification.Api"]);
        options.ChannelOptionsActions.Add(channel =>
        {
            channel.Credentials = ChannelCredentials.Insecure;
            channel.ServiceConfig = new ServiceConfig { MethodConfigs = { defaultMethodConfig } };
        });
    })
    .AddCorrelationIdForwarding();

builder.Services
    .AddGrpcClient<InfrastructureService.InfrastructureServiceClient>(options =>
    {
        options.Address = new Uri(builder.Configuration["Urls:People.Api"]);
        options.ChannelOptionsActions.Add(channel =>
        {
            channel.Credentials = ChannelCredentials.Insecure;
            channel.ServiceConfig = new ServiceConfig { MethodConfigs = { defaultMethodConfig } };
        });
    })
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

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        ForwardLimit = 10
    })
    .UseCors(mainCors)
    .UseCorrelationId()
    .UseRouting()
    .UseAuthentication()
    .UseAuthorization()
    .UsePeopleLocalization();

app.MapControllers();

app.Run();
