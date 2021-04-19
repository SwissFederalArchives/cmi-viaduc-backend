using Ninject;
using Quartz;
using Quartz.Spi;

namespace CMI.Manager.DataFeed.Infrastructure
{
    /// <summary>
    ///     JobFactory resolves dependencies for the Quartz scheduler
    /// </summary>
    internal class JobFactory : IJobFactory
    {
        private readonly IKernel kernel;

        public JobFactory(IKernel kernel)
        {
            this.kernel = kernel;
        }

        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            return (IJob) kernel.Get(bundle.JobDetail.JobType);
        }

        public void ReturnJob(IJob job)
        {
            kernel.Get(job.GetType());
        }
    }
}