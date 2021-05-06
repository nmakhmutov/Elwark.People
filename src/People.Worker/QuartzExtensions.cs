using Quartz;

namespace People.Worker
{
    public static class QuartzExtensions
    {
        public static IServiceCollectionQuartzConfigurator AddJobAndTrigger<T>(
            this IServiceCollectionQuartzConfigurator quartz, string cronExpression)
            where T : IJob
        {
            var jobName = typeof(T).Name;
            var jobKey = new JobKey(jobName);
            quartz.AddJob<T>(configurator => configurator.WithIdentity(jobKey));

            quartz.AddTrigger(configurator => configurator
                .ForJob(jobKey)
                .WithIdentity($"{jobName}Trigger")
                .WithCronSchedule(cronExpression)
            );

            return quartz;
        }
    }
}