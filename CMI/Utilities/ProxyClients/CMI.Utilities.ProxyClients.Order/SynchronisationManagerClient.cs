using CMI.Contract.Common;
using CMI.Contract.Common.Entities;
using CMI.Contract.Messaging;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMI.Utilities.ProxyClients.Order;

public class SynchronisationManagerClient : ISynchronisationManager
{
    private readonly IBus bus;

    public SynchronisationManagerClient(IBus bus)
    {
        this.bus = bus;
    }

    public async Task<bool> BatchAddSyncActions(string[] ids, int action)
    {
        var client = GetRequestClient<BatchAddSyncActionsRequest>(requestTimeOutInSeconds: 3600);
        var result = await client.GetResponse<BatchAddSyncActionsResponse>(new BatchAddSyncActionsRequest
        {
            Ids = ids,
            Action = action
        });
        if( result.Message.Ok)
        {
            var endpoint = await bus.GetSendEndpoint(new Uri(bus.Address, BusConstants.DatafeedSchedulerTriggerMessageQueue));
            await endpoint.Send<ISchedulerTrigger>( new SchedulerTrigger());
        }
        return result.Message.Ok;
    }

    public async Task<List<VSyncNumberPerHourDto>> GetSyncNumberPerHour(int days)
    {
        var client = GetRequestClient<GetSyncNumberPerHourRequest>();
        var result = await client.GetResponse<GetSyncNumberPerHourResponse>(new GetSyncNumberPerHourRequest
        {
            FilterDays = days
        });
        return result.Message.SyncNumberPerHourItems;
    }

    public async Task<List<SyncActionLogDto>> GetLogData(long syncActionId)
    {
        var client = GetRequestClient<GetLogDataRequest>();
        var result = await client.GetResponse<GetLogDataResponse>(new GetLogDataRequest
        {
            SyncActionId = syncActionId
        });
        return result.Message.LogData;
    }

    public async Task<List<SyncActionDto>> GetSyncData(int filterId)
    {
        var client = GetRequestClient<GetSyncDataRequest>();
        var result = await client.GetResponse<GetSyncDataResponse>(new GetSyncDataRequest
        {
            FilterId = filterId
        });
        return result.Message.SyncData;
    }

    private IRequestClient<T1> GetRequestClient<T1>(string queueEndpoint = "", int requestTimeOutInSeconds = 0) where T1 : class
    {
        var serviceUrl = string.IsNullOrEmpty(queueEndpoint)
            ? string.Format(BusConstants.ViaducManagerRequestBase, typeof(T1).Name)
            : queueEndpoint;

#if DEBUG
        var requestTimeout = TimeSpan.FromSeconds(600);
#else
                var requestTimeout = TimeSpan.FromSeconds(300);
#endif

        if (requestTimeOutInSeconds > 0)
        {
            requestTimeout = TimeSpan.FromSeconds(requestTimeOutInSeconds);
        }

        return bus.CreateRequestClient<T1>(new Uri(bus.Address, serviceUrl), requestTimeout);
    }

}