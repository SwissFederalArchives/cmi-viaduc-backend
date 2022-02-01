using Autofac;
using Quartz;
using Quartz.Spi;

namespace CMI.Manager.DataFeed.Infrastructure
{
    /// <summary>
    ///     JobFactory resolves dependencies for the Quartz scheduler
    /// </summary>
    internal class JobFactory : IJobFactory
    {
        private readonly IContainer container;

        public JobFactory(IContainer container)
        {
            this.container = container;
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            return (IJob) container.Resolve(bundle.JobDetail.JobType);
        }

        public void ReturnJob(IJob job)
        {
            container.Resolve(job.GetType());
        }
    }
}