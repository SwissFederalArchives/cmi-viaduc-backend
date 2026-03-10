using System;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using MassTransit;

namespace CMI.Manager.Harvest.SyncLog;

public class DbSyncLogAccess : IDbSyncLogAccess
{
    private readonly IBus bus;

    public DbSyncLogAccess(IBus bus)
    {
        this.bus = bus;
    }

    public async Task<int> UpdateMutationStatus(MutationStatusInfo info)
    {
        var client = GetRequestClient<UpdateMutationStatusRequest>();
        var result = await client.GetResponse<UpdateMutationStatusResponse>(new UpdateMutationStatusRequest
        {
            Info = info
        });
        return result.Message.Result;
    }

    private IRequestClient<T1> GetRequestClient<T1>(string queueEndpoint = "", int requestTimeOutInSeconds = 0) where T1 : class
    {
        var serviceUrl = string.IsNullOrEmpty(queueEndpoint)
            ? string.Format(BusConstants.ViaducManagerRequestBase, typeof(T1).Name)
            : queueEndpoint;

#if DEBUG
        var requestTimeout = TimeSpan.FromSeconds(120);
#else
                var requestTimeout = TimeSpan.FromSeconds(60);
#endif

        if (requestTimeOutInSeconds > 0)
        {
            requestTimeout = TimeSpan.FromSeconds(requestTimeOutInSeconds);
        }

        return bus.CreateRequestClient<T1>(new Uri(bus.Address, serviceUrl), requestTimeout);
    }

}