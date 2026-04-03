using System.Net;
using FluentValidation;
using Fluid;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using People.Application.Behaviour;
using People.Domain.Entities;
using People.Domain.ValueObjects;
using People.Infrastructure;
using People.Worker.Jobs;
using Quartz;
using TimeZone = People.Domain.ValueObjects.TimeZone;

const string appName = "People.Worker";
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .Configure<HostOptions>(options =>
    {
        options.ServicesStartConcurrently = true;
        options.ServicesStopConcurrently = true;
    });

builder.Services
    .AddHealthChecks();

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

builder.Services.AddQuartz(options =>
    {
        options.ScheduleJob<OutboxDispatchJob>(trigger => trigger
            .WithIdentity(nameof(OutboxDispatchJob))
            .StartAt(DateBuilder.EvenMinuteDateAfterNow())
            .WithSimpleSchedule(x => x.WithIntervalInSeconds(3).RepeatForever())
        );

        options.ScheduleJob<WebhookDispatchJob>(trigger => trigger
            .WithIdentity(nameof(WebhookDispatchJob))
            .StartAt(DateBuilder.EvenMinuteDateAfterNow())
            .WithSimpleSchedule(x => x.WithIntervalInSeconds(15).RepeatForever())
        );

        options.ScheduleJob<OutboxCleanupJob>(trigger => trigger
            .WithIdentity(nameof(OutboxCleanupJob))
            .StartAt(DateBuilder.TodayAt(14, 0, 0))
            .WithSimpleSchedule(s => s.WithIntervalInHours(24).RepeatForever())
        );

        options.ScheduleJob<ConfirmationCleanupJob>(trigger => trigger
            .WithIdentity(nameof(ConfirmationCleanupJob))
            .StartAt(DateBuilder.EvenMinuteDateAfterNow())
            .WithSimpleSchedule(x => x.WithIntervalInMinutes(1).RepeatForever())
        );
    })
    .AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

builder.AddSerilog(appName, configuration => configuration
    .Destructure.AsScalar<AccountId>()
    .Destructure.AsScalar<Language>()
    .Destructure.AsScalar<RegionCode>()
    .Destructure.AsScalar<CountryCode>()
    .Destructure.AsScalar<TimeZone>()
    .Destructure.AsScalar<DateFormat>()
    .Destructure.AsScalar<TimeFormat>()
    .Destructure.AsScalar<IPAddress>()
    .Destructure.ByTransforming<Account>(x => new { x.Id, x.Name.Nickname })
);

await using var app = builder.Build();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = static _ => false,
    ResponseWriter = static (context, report) => context.Response.WriteAsJsonAsync(report)
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = static _ => true,
    ResponseWriter = static (context, report) => context.Response.WriteAsJsonAsync(report)
});

await app.RunAsync();

public partial class Program;
