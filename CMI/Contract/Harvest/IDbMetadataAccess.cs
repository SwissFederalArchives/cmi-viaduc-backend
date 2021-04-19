using CMI.Contract.Common;

namespace CMI.Contract.Harvest
{
    public interface IDbMetadataAccess
    {
        /// <summary>
        ///     Gets an archive record from the AIS.
        /// </summary>
        /// <param name="archiveRecordId">The primary key id of the record in the AIS as a string.</param>
        /// <returns>ArchiveRecord.</returns>
        ArchiveRecord GetArchiveRecord(string archiveRecordId);
    }
}