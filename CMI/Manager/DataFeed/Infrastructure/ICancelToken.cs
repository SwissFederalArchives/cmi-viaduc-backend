namespace CMI.Manager.DataFeed.Infrastructure
{
    public interface ICancelToken
    {
        bool JobTerminationRequested { get; }

        void Cancel();
    }
}