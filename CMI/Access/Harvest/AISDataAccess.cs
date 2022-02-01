using System.Collections.Generic;
using CMI.Access.Harvest.ScopeArchiv;
using CMI.Contract.Common;
using CMI.Contract.Harvest;

namespace CMI.Access.Harvest
{
    /// <summary>
    ///     DB access class for getting the data out of the AIS (archive information system)
    ///     Switching the record builder and data provider, you'll be able to get the data from a different AIS.
    ///     Implemented as partial class where some interfaces are declared in the respective partial file, to make it
    ///     more obvious which parts belong to which interface.
    /// </summary>
    public partial class AISDataAccess : IDbMutationQueueAccess, IDbMetadataAccess
    {
        private readonly IAISDataProvider dataProvider;
        private readonly ArchiveRecordBuilder recordBuilder;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AISDataAccess" /> class.
        /// </summary>
        /// <param name="recordBuilder">The archive record build</param>
        /// <param name="dataProvider">The data provider.</param>
        public AISDataAccess(ArchiveRecordBuilder recordBuilder, DigitizationOrderBuilder digitizationOrderBuilder, IAISDataProvider dataProvider)
        {
            this.recordBuilder = recordBuilder;
            this.digitizationOrderBuilder = digitizationOrderBuilder;
            this.dataProvider = dataProvider;
        }

        /// <summary>
        ///     Gets an archive record from the AIS.
        /// </summary>
        /// <param name="archiveRecordId">The primary key id of the record in the AIS as a string.</param>
        /// <returns>ArchiveRecord.</returns>
        ArchiveRecord IDbMetadataAccess.GetArchiveRecord(string archiveRecordId)
        {
            return recordBuilder.Build(archiveRecordId);
        }

        /// <summary>
        ///     Gets the pending mutations from the AIS.
        /// </summary>
        /// <returns>A list with the records that need to be synced.</returns>
        public List<MutationRecord> GetPendingMutations()
        {
            return dataProvider.GetPendingMutations();
        }

        /// <summary>
        ///     Updates the mutation status of a mutation record in the AIS.
        /// </summary>
        /// <param name="info">Object with detailed information about the status change.</param>
        /// <returns>The number of affected records.</returns>
        public int UpdateMutationStatus(MutationStatusInfo info)
        {
            return dataProvider.UpdateMutationStatus(info);
        }

        /// <summary>Makes a bulk update of the mutation status in the AIS.</summary>
        /// <param name="infos">List ob objects with detailed information about the status change.</param>
        /// <returns>The number of affected records.</returns>
        public int BulkUpdateMutationStatus(List<MutationStatusInfo> infos)
        {
            return dataProvider.BulkUpdateMutationStatus(infos);
        }

        /// <summary>Reset failed or lost synchronize operations in the mutation table to the initial status.</summary>
        /// <param name="maxRetries">Maximum number of times a failed operation is reset.</param>
        /// <returns>Number of records that were reset.</returns>
        public int ResetFailedSyncOperations(int maxRetries)
        {
            return dataProvider.ResetFailedSyncOperations(maxRetries);
        }
    }
}