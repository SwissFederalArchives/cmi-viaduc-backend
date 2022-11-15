using System.Linq;
using CMI.Access.Common;
using CMI.Contract.Common;

namespace CMI.Engine.Anonymization
{
    public class AnonymizationReferenceEngine: IAnonymizationReferenceEngine
    {
        private readonly ISearchIndexDataAccess dbAccess;

        public AnonymizationReferenceEngine(ISearchIndexDataAccess dbAccess)
        {
            this.dbAccess = dbAccess;
        }

        /// <summary>
        /// Updates the title of anonymized records in dependent records such as
        /// - References
        /// - Archiveplan of children
        /// - ParentContentInfos of children
        /// </summary>
        /// <param name="elasticArchiveDbRecord">The db record containing the sources for the update</param>
        public void UpdateDependentRecords(ElasticArchiveDbRecord elasticArchiveDbRecord)
        {
            SyncArchiveplanAndParentContentInfos(elasticArchiveDbRecord);
            SyncReferences(elasticArchiveDbRecord);
        }

        /// <summary>
        /// Updates the title field of the ArchivePlanContext and ParentContentInfos of itself
        /// </summary>
        /// <param name="elasticArchiveRecord">The record to modify</param>
        public void UpdateSelf(ElasticArchiveDbRecord elasticArchiveRecord)
        {
            UpdateArchivePlanContext(elasticArchiveRecord, elasticArchiveRecord);
            UpdateParentContentInfos(elasticArchiveRecord, elasticArchiveRecord);
            SyncOwnReferences(elasticArchiveRecord);
        }

        public void UpdateReferencesOfUnprotectedRecord(ElasticArchiveDbRecord elasticArchiveRecord)
        {
            SyncOwnReferences(elasticArchiveRecord);
        }

        private void SyncReferences(ElasticArchiveDbRecord elasticArchiveRecord)
        {
            foreach (var reference in elasticArchiveRecord.References)
            {
                // Get the referenced record
                var refRecord = dbAccess.FindDbDocument(reference.ArchiveRecordId, true);
                if (refRecord != null)
                {
                    // Find the reference 
                    var backRef = refRecord.References.FirstOrDefault(r => r.ArchiveRecordId == elasticArchiveRecord.ArchiveRecordId);
                    if (backRef != null)
                    {
                        backRef.ReferenceName = GetReferenceName(elasticArchiveRecord);
                        dbAccess.UpdateDocument(refRecord);
                    }
                }
            }
        }

        private void SyncOwnReferences(ElasticArchiveDbRecord elasticArchiveRecord)
        {
            var wasUpdated = false;
            foreach (var reference in elasticArchiveRecord.References.Where(r => r.Protected))
            {
                // Get the referenced record
                var refRecord = dbAccess.FindDbDocument(reference.ArchiveRecordId, true);
                if (refRecord != null)
                {
                    // Update the reference 
                    reference.ReferenceName = GetReferenceName(refRecord);
                    wasUpdated = true;
                }
            }

            if (wasUpdated)
            {
                dbAccess.UpdateDocument(elasticArchiveRecord);
            }
        }

        private void SyncArchiveplanAndParentContentInfos(ElasticArchiveDbRecord elasticArchiveRecord)
        {
            // Get all children
            var children = dbAccess.GetChildren(elasticArchiveRecord.ArchiveRecordId, true).ToList();
            foreach (var child in children)
            {
                // Get the corresponding db record
                var childDbRecord = dbAccess.FindDbDocument(child.ArchiveRecordId, false);
                if (childDbRecord != null)
                {
                    UpdateArchivePlanContext(childDbRecord, elasticArchiveRecord);
                    UpdateParentContentInfos(childDbRecord, elasticArchiveRecord);
                    dbAccess.UpdateDocument(childDbRecord);
                }
            }
        }

        private void UpdateParentContentInfos(ElasticArchiveDbRecord child, ElasticArchiveDbRecord source)
        {
            // The problem with the parent content infos is, that we do not have an id to identify the exact item.
            // But what is always true is, that the parent hierachy of the parent is the same as the one of the child.
            // So we can loop through the items of the archive plan context and update the same indexes of the child.
            for (var index = 0; index < source.ArchiveplanContext.Count; index++)
            {
                var contentInfo = source.ArchiveplanContext[index];
                var childInfo = child.ParentContentInfos[index];
                if (childInfo != null)
                {
                    childInfo.Title = contentInfo.Title;
                }
            }

            // Update also the data in the unanonymizedFields
            // As there is the possibility that a child might not be anonymized and not have the UnanonymizedFields initialized
            // we have to test for that condition
            if (child.UnanonymizedFields.ParentContentInfos.Any() && source.UnanonymizedFields.ParentContentInfos.Any())
            {
                for (var index = 0; index < source.UnanonymizedFields.ArchiveplanContext.Count; index++)
                {
                    var contentInfo = source.UnanonymizedFields.ArchiveplanContext[index];
                    var childInfo = child.UnanonymizedFields.ParentContentInfos[index];
                    if (childInfo != null)
                    {
                        childInfo.Title = contentInfo.Title;
                    }
                }
            }
        }

        private void UpdateArchivePlanContext(ElasticArchiveDbRecord child, ElasticArchiveDbRecord source)
        {
            var parentItem = child.ArchiveplanContext.FirstOrDefault(c => c.ArchiveRecordId == source.ArchiveRecordId);
            if (parentItem != null)
            {
                parentItem.Title = source.Title;
            }

            // Also update the data in the unanonymizedFields
            if (child.UnanonymizedFields.ParentContentInfos.Any() && source.UnanonymizedFields.ParentContentInfos.Any())
            {
                var unanonymizedItem = child.UnanonymizedFields.ArchiveplanContext.FirstOrDefault(c => c.ArchiveRecordId == source.ArchiveRecordId);
                if (unanonymizedItem != null)
                {
                    unanonymizedItem.Title = source.UnanonymizedFields.Title;
                }
            }
        }

        private static string GetReferenceName(ElasticArchiveDbRecord elasticArchiveRecord)
        {
            return $"{elasticArchiveRecord.ReferenceCode} {elasticArchiveRecord.Title}{(elasticArchiveRecord.CreationPeriod != null ? ", " + elasticArchiveRecord.CreationPeriod.Text : "")} ({elasticArchiveRecord.Level})";
        }
    }
}
