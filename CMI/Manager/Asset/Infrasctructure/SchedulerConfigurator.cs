using System;
using System.Threading.Tasks;
using CMI.Manager.Asset.Jobs;
using CMI.Manager.Asset.ParameterSettings;
using Ninject;
using Quartz;
using Quartz.Impl;
using Serilog;

namespace CMI.Manager.Asset.Infrasctructure
{
    internal static class SchedulerConfigurator
    {
        public static async Task<IScheduler> Configure(IKernel kernel)
        {
            var scheduler = await StdSchedulerFactory.GetDefaultScheduler();
            scheduler.JobFactory = new JobFactory(kernel);
            var settings = kernel.Get<AssetPriorisierungSettings>();

            // Define the standard job to get the pending SYNC records
            // -------------------------------------------------------
            Log.Verbose("Start creation of CheckPendingSyncRecordsJob");
            var job = JobBuilder.Create<CheckPendingSyncRecordsJob>()
                .WithIdentity("checkPendingSyncRecordsJob", "standardGroup")
                .Build();

            // Trigger the job to run now, and then repeat every x seconds
            var trigger = TriggerBuilder.Create()
                .WithIdentity("checkPendingSyncRecordsTrigger", "standardGroup")
                .StartAt(DateTime.Now.AddSeconds(2)) // Allows for initialization of the bus
                .WithCronSchedule(settings.CheckAuftraegeJobIntervalAsCron)
                .Build();

            // register the job using the trigger
            await scheduler.ScheduleJob(job, trigger);
            Log.Verbose("Finished creation of CheckPendingSyncRecordsJob and trigger");

            Log.Verbose("Start creation of DeleteOldRecordsJob");
            job = JobBuilder.Create<DeleteOldRecordsJob>()
                .WithIdentity("deleteOldRecordsJob", "standardGroup")
                .Build();

            // Trigger the job to run now, and then repeat every x seconds
            trigger = TriggerBuilder.Create()
                .WithIdentity("deleteOldRecordsJobTrigger", "standardGroup")
                .StartAt(DateTime.Now.AddSeconds(2)) // Allows for initialization of the bus
                .WithCronSchedule(settings.AlteAuftraegeLoeschenJobIntervalAsCron)
                .Build();

            // register the job using the trigger
            await scheduler.ScheduleJob(job, trigger);


            // Define the standard job to get the pending DOWNLOAD records
            // -----------------------------------------------------------
            Log.Verbose("Start creation of CheckPendingDownloadRecordsJob");
            job = JobBuilder.Create<CheckPendingDownloadRecordsJob>()
                .WithIdentity("checkPendingDownloadRecordsJob", "standardGroup")
                .Build();

            // Trigger the job to run now, and then repeat every x seconds
            trigger = TriggerBuilder.Create()
                .WithIdentity("checkPendingDownloadRecordsTrigger", "standardGroup")
                .StartAt(DateTime.Now.AddSeconds(2)) // Allows for initialization of the bus
                .WithCronSchedule(settings.CheckAuftraegeJobIntervalAsCron)
                .Build();

            // register the job using the trigger
            await scheduler.ScheduleJob(job, trigger);
            Log.Verbose("Finished creation of CheckPendingDownloadRecordsJob and trigger");
            Log.Verbose("Finished scheduler configuration");

            return scheduler;
        }
    }
}