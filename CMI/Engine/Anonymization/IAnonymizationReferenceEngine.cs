
using CMI.Contract.Common;

namespace CMI.Engine.Anonymization
{
    public interface IAnonymizationReferenceEngine
    {
        /// <summary>
        /// Updates the title of anonymized records in dependent records such as
        /// - References
        /// - Archiveplan of children
        /// - ParentContentInfos of children
        /// </summary>
        /// <param name="elasticArchiveDbRecord">The db record containing the sources for the update</param>
        void UpdateDependentRecords(ElasticArchiveDbRecord elasticArchiveDbRecord);

        /// <summary>
        /// Updates the title field of the ArchivePlanContext and ParentContentInfos of itself
        /// </summary>
        /// <param name="elasticArchiveRecord">The record to modify</param>
        void UpdateSelf(ElasticArchiveDbRecord elasticArchiveRecord);

        /// <summary>
        /// Updates the references of a unprotected record that might have references to a protected record
        /// </summary>
        /// <param name="elasticArchiveRecord"></param>
        void UpdateReferencesOfUnprotectedRecord(ElasticArchiveDbRecord elasticArchiveRecord);
    }
}
