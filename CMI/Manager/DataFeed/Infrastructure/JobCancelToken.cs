namespace CMI.Manager.DataFeed.Infrastructure
{
    public class JobCancelToken : ICancelToken
    {
        public bool JobTerminationRequested { get; private set; }

        public void Cancel()
        {
            JobTerminationRequested = true;
        }
    }
}