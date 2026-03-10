using System.Threading.Tasks;
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
    public partial class AISDataAccess : IDbMetadataAccess
    {
        private readonly IAISDataProvider aisDataProvider;
        private readonly IArchiveRecordBuilder recordBuilder;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AISDataAccess" /> class.
        /// </summary>
        /// <param name="recordBuilder">The archive record build</param>
        /// <param name="digitizationOrderBuilder"></param>
        /// <param name="aisDataProvider">The data provider for the AIS.</param>
        public AISDataAccess(IArchiveRecordBuilder recordBuilder, IDigitizationOrderBuilder digitizationOrderBuilder, IAISDataProvider aisDataProvider)
        {
            this.recordBuilder = recordBuilder;
            this.digitizationOrderBuilder = digitizationOrderBuilder;
            this.aisDataProvider = aisDataProvider;
        }

        #region IDbMetadataAccess

        /// <summary>
        ///     Gets an archive record from the AIS.
        /// </summary>
        /// <param name="archiveRecordId">The primary key id of the record in the AIS as a string.</param>
        /// <returns>ArchiveRecord.</returns>
        async Task<ArchiveRecord> IDbMetadataAccess.GetArchiveRecord(string archiveRecordId)
        {
            return await recordBuilder.Build(archiveRecordId);
        }

        /// <summary>
        ///     Gets an ais access tokens from the AIS.
        /// </summary>
        /// <param name="archiveRecordId">The primary key id of the record in the AIS as a string.</param>
        /// <returns>ArchiveRecord.</returns>
        async Task<ArchiveRecordSecurity> IDbMetadataAccess.GetAisAccessTokens(string archiveRecordId)
        {
            return await recordBuilder.BuildSecurityTokens(archiveRecordId);
        }

        
        #endregion
    }
}