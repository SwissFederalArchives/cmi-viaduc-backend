using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Harvest;
using CMI.Contract.Messaging;
using CMI.Manager.DataFeed.Infrastructure;
using MassTransit;
using Quartz;
using Serilog;

namespace CMI.Manager.DataFeed
{
    /// <summary>
    ///     Checks the mutation queue.
    /// </summary>
    /// <seealso cref="Quartz.IJob" />
    public class CheckMutationQueueJob : IJob
    {
        private static bool isEnqueing;
        private readonly IBus bus;
        private readonly ICancelToken cancelToken;
        private readonly IDbMutationQueueAccess dbMutationQueueAccess;

        /// <summary>
        ///     Initializes a new instance of the <see cref="CheckMutationQueueJob" /> class.
        /// </summary>
        /// <param name="bus">A reference to the bus.</param>
        /// <param name="dbMutationQueueAccess">The db access class.</param>
        /// <param name="cancelToken">A token for canceling a (long) running check process.</param>
        public CheckMutationQueueJob(IBus bus, IDbMutationQueueAccess dbMutationQueueAccess, ICancelToken cancelToken)
        {
            this.bus = bus;
            this.dbMutationQueueAccess = dbMutationQueueAccess;
            this.cancelToken = cancelToken;
        }


        /// <summary>
        ///     Called by the <see cref="T:Quartz.IScheduler" /> when a <see cref="T:Quartz.ITrigger" />
        ///     fires that is associated with the <see cref="T:Quartz.IJob" />.
        /// </summary>
        /// <param name="context">The execution context.</param>
        /// <remarks>
        ///     The implementation may wish to set a  result object on the
        ///     JobExecutionContext before this method exits.  The result itself
        ///     is meaningless to Quartz, but may be informative to
        ///     <see cref="T:Quartz.IJobListener" />s or
        ///     <see cref="T:Quartz.ITriggerListener" />s that are watching the job's
        ///     execution.
        /// </remarks>
        public async Task Execute(IJobExecutionContext context)
        {
            if (!isEnqueing)
            {
                Log.Information("Checking pending mutations in the AIS");

                try
                {
                    isEnqueing = true;
                    var list = dbMutationQueueAccess.GetPendingMutations();
                    if (list.Any())
                    {
                        Log.Information("About to push {itemCout} items onto the bus.", list.Count);
                        var endpoint = await bus.GetSendEndpoint(new Uri(bus.Address, BusConstants.HarvestManagerSyncArchiveRecordMessageQueue));
                        var currentCount = 0;
                        var totalCount = list.Count;
                        var updateList = new List<MutationStatusInfo>();
                        Task dummyTask;
                        foreach (var record in list)
                        {
                            // Check if termination was requested
                            if (cancelToken.JobTerminationRequested)
                            {
                                break;
                            }

                            // Add the info to a list for later update
                            updateList.Add(new MutationStatusInfo
                            {
                                MutationId = record.MutationId, NewStatus = ActionStatus.SyncInProgress,
                                ChangeFromStatus = ActionStatus.WaitingForSync
                            });
                            // We don't await the task. It is a great performance gain, with a little risk that the 
                            // message might not be acknowledged.
                            dummyTask = endpoint.Send<ISyncArchiveRecord>(new
                            {
                                record.MutationId,
                                record.ArchiveRecordId,
                                record.Action
                            });

                            currentCount++;
                            if (currentCount % 10000 == 0)
                            {
                                Log.Information($"Initiated sync for {currentCount} of {totalCount}");
                                dbMutationQueueAccess.BulkUpdateMutationStatus(updateList);
                                updateList.Clear();
                            }

                            Log.Verbose("Put mutation record with mutationId {MutationId} on the bus", record.MutationId);
                        }

                        Log.Information($"Initiated sync for {currentCount} of {totalCount}");
                        dbMutationQueueAccess.BulkUpdateMutationStatus(updateList);
                        Log.Information("Finished to put items on queue.");
                    }
                    else
                    {
                        Log.Information("No pending mutation records found in the mutation queue");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, ex.Message);
                }
                finally
                {
                    isEnqueing = false;
                }
            }
            else
            {
                Log.Warning("Skipping timer job, as former trigger is still executing");
            }
        }
    }
}