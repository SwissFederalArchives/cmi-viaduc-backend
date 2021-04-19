using System;
using System.Threading.Tasks;
using CMI.Manager.DataFeed.Properties;
using Ninject;
using Quartz;
using Quartz.Impl;
using Serilog;

namespace CMI.Manager.DataFeed.Infrastructure
{
    internal static class SchedulerConfigurator
    {
        public static async Task<IScheduler> Configure(IKernel kernel)
        {
            var scheduler = await StdSchedulerFactory.GetDefaultScheduler();
            scheduler.JobFactory = new JobFactory(kernel);

            // Define the standard job to get the pending records
            // --------------------------------------------------
            Log.Verbose("Start creation of CheckMutationQueueJob");
            var job = JobBuilder.Create<CheckMutationQueueJob>()
                .WithIdentity("checkMutationQueueJob", "standardGroup")
                .Build();

            // Trigger the job to run now, and then repeat every x seconds
            var trigger = TriggerBuilder.Create()
                .WithIdentity("checkMutationQueueTrigger", "standardGroup")
                .StartAt(DateTime.Now.AddSeconds(2)) // Allows for initialization of the bus
                .WithSimpleSchedule(x => x
                    .WithIntervalInSeconds(Settings.Default.CheckQueueIntervalInSeconds)
                    .RepeatForever())
                .Build();

            // register the job using the trigger
            await scheduler.ScheduleJob(job, trigger);
            Log.Verbose("Finished creation of CheckMutationQueueJob and trigger");

            if (Settings.Default.RequeueJobIntervalInSeconds > 0)
            {
                // Define the job to requeue failed or lost sync op.
                // --------------------------------------------------
                Log.Verbose("Start creation of RequeueMutationJob");
                job = JobBuilder.Create<RequeueMutationJob>()
                    .WithIdentity("requeueMutationJob", "standardGroup")
                    .Build();

                // Trigger the job to run now, and then repeat every x seconds
                trigger = TriggerBuilder.Create()
                    .WithIdentity("requeueMutationTrigger", "standardGroup")
                    .StartAt(DateTime.Now.AddSeconds(2)) // Allows for initialization of the bus
                    .WithSimpleSchedule(x => x
                        .WithIntervalInSeconds(Settings.Default.RequeueJobIntervalInSeconds)
                        .RepeatForever())
                    .Build();

                // Tell quartz to schedule the job using our trigger
                await scheduler.ScheduleJob(job, trigger);
                Log.Verbose("Finished creation of RequeueMutationJob");
            }

            Log.Verbose("Finished scheduler configuration");
            return scheduler;
        }
    }
}