using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Common.Entities;
using CMI.Contract.Messaging;
using MassTransit;

namespace CMI.Manager.DataFeed.SyncLog;

public class DbSyncLogAccess : IDbSyncLogAccess
{
    private readonly IBus bus;

    public DbSyncLogAccess(IBus bus)
    {
        this.bus = bus;
    }

    public async Task<List<MutationRecord>> GetPendingMutations()
    {
        var client = GetRequestClient<GetPendingMutationsRequest>();
        var result = await client.GetResponse<GetPendingMutationsResponse>(new GetPendingMutationsRequest());
        return result.Message.MutationRecords;
    }

    public async Task<int> BulkUpdateMutationStatus(List<MutationStatusInfo> infos)
    {
        var client = GetRequestClient<BulkUpdateMutationStatusRequest>();
        var result = await client.GetResponse<BulkUpdateMutationStatusResponse>(new BulkUpdateMutationStatusRequest
        {
            Infos = infos
        });
        return result.Message.Result;
    }

    public async Task<int> ResetFailedSyncOperations(int maxRetries)
    {
        var client = GetRequestClient<ResetFailedSyncOperationsRequest>();
        var result = await client.GetResponse<ResetFailedSyncOperationsResponse>(new ResetFailedSyncOperationsRequest
        {
            MaxRetries = maxRetries
        });
        return result.Message.Result;
    }

    public async Task InsertSyncActionAsync(SyncActionDto syncActionDto)
    {
        var client = GetRequestClient<InsertSyncActionRequest>();
        await client.GetResponse<InsertSyncActionResponse>(new InsertSyncActionRequest
        {
            SyncActionDto = syncActionDto
        });
    }

    public async Task DeleteOldSyncActionAsync(int daysAgo)
    {
        var client = GetRequestClient<DeleteOldSyncActionRequest>();
        await client.GetResponse<InsertSyncActionResponse>(new DeleteOldSyncActionRequest
        {
            DaysAgo = daysAgo
        });
    }

    private IRequestClient<T1> GetRequestClient<T1>(string queueEndpoint = "", int requestTimeOutInSeconds = 0) where T1 : class
    {
        var serviceUrl = string.IsNullOrEmpty(queueEndpoint)
            ? string.Format(BusConstants.ViaducManagerRequestBase, typeof(T1).Name)
            : queueEndpoint;
        var requestTimeout = TimeSpan.FromMinutes(10);  // No db operation should take longer than 10 minutes

        if (requestTimeOutInSeconds > 0)
        {
            requestTimeout = TimeSpan.FromSeconds(requestTimeOutInSeconds);
        }

        return bus.CreateRequestClient<T1>(new Uri(bus.Address, serviceUrl), requestTimeout);
    }

}